import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

export const options = {
  scenarios: {
    seeding: {
      executor: 'constant-vus',
      vus: 5,
      duration: '3m', // ajustar según SEED_COUNT
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    checks: ['rate>0.90']
  },
  tags: { test_type: 'seed', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';
const SEED_COUNT = parseInt(__ENV.SEED_COUNT || '100', 10);
const PREFIX = __ENV.DATASET_PREFIX || 'Seed';

const created = new Counter('datasets_created');

function login() {
  if (__ENV.TOKEN) return __ENV.TOKEN;
  const res = http.post(`${BASE_URL}/api/Auth/login`, JSON.stringify({ username: USER, password: PASS }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'auth_login' }
  });
  check(res, { 'login 200': r => r.status === 200 });
  return res.json('token');
}

export function setup() {
  const token = login();
  return { token };
}

let globalCount = 0;

export default function (data) {
  if (globalCount >= SEED_COUNT) {
    sleep(2); // idle until scenario ends
    return;
  }
  const name = `${PREFIX}_${globalCount}_${Math.random().toString(36).slice(2, 8)}`;
  const payload = {
    name,
    username: USER,
    description: 'Dataset generado para pruebas de carga',
    isDataset: 'S',
    contentType: '0',
    sourceId: null,
    groupId: null,
    sensorName: null,
    deviceIds: []
  };
  const res = http.post(`${BASE_URL}/api/Dataset`, JSON.stringify(payload), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'dataset_create' }
  });
  const ok = check(res, { 'create ok': r => r.status === 201 || r.status === 200 });
  if (ok) created.add(1);
  globalCount++;
  sleep(0.2);
}
