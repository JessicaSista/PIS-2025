import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Counter } from 'k6/metrics';

export const options = {
  scenarios: {
    baseline: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 5 },
        { duration: '1m', target: 10 },
        { duration: '1m', target: 15 },
        { duration: '30s', target: 0 }
      ],
      gracefulRampDown: '10s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<800'],
    checks: ['rate>0.97'],
    'dataset_create_duration{endpoint:dataset_create}': ['p(95)<900']
  },
  tags: { test_type: 'baseline', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';

const createTrend = new Trend('dataset_create_duration');
const listTrend = new Trend('dataset_list_duration');
const createErrors = new Counter('dataset_create_errors');

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
  const token = data.token;

  group('listar datasets', () => {
    const res = http.get(`${BASE_URL}/api/Dataset/user?token=${token}`, { tags: { endpoint: 'dataset_list' } });
    listTrend.add(res.timings.duration);
    check(res, {
      'list 200': r => r.status === 200,
      'list is array': r => Array.isArray(r.json())
    });
  });

  group('crear dataset', () => {
    const payload = {
      name: `Perf_${Date.now()}_${Math.random().toString(16).slice(2,8)}`,
      username: USER,
      description: 'Dataset creado por baseline test',
      isDataset: 'S',
      contentType: '0',
      sourceId: null,
      groupId: null,
      sensorName: null,
      deviceIds: []
    };

    const headers = { 'Content-Type': 'application/json' };
    const res = http.post(`${BASE_URL}/api/Dataset`, JSON.stringify(payload), { headers, tags: { endpoint: 'dataset_create' } });
    createTrend.add(res.timings.duration);
    const ok = check(res, {
      'create 201/200': r => r.status === 201 || r.status === 200,
      'dataset id presente': r => !!r.json('id')
    });
    if (!ok) createErrors.add(1);
  });

  sleep(Math.random() + 0.5);
}
