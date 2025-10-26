import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter } from 'k6/metrics';

export const options = {
  vus: 5,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.03'],
    http_req_duration: ['p(95)<900'],
    'create_only_duration{endpoint:dataset_create}': ['p(95)<850']
  },
  tags: { test_type: 'create_only', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';
const createTrend = new Trend('create_only_duration');
const createErrors = new Counter('create_only_errors');

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

export default function (data) {
  const payload = {
    name: `CreateOnly_${Date.now()}_${Math.random().toString(16).slice(2,8)}`,
    username: USER,
    description: 'Create only test dataset',
    isDataset: 'S',
    contentType: '0'
  };
  const res = http.post(`${BASE_URL}/api/Dataset`, JSON.stringify(payload), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'dataset_create' }
  });
  createTrend.add(res.timings.duration);
  const ok = check(res, { 'create ok': r => r.status === 201 || r.status === 200 });
  if (!ok) createErrors.add(1);
  sleep(0.4);
}
