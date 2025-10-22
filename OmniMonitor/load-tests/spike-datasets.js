import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    spike: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 0 }, // warmup
        { duration: '10s', target: 60 }, // spike rápido
        { duration: '30s', target: 60 }, // mantener
        { duration: '10s', target: 0 } // caída
      ],
      gracefulRampDown: '10s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.07'],
    http_req_duration: ['p(95)<2500']
  },
  tags: { test_type: 'spike', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';

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
  const list = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`, { tags: { endpoint: 'dataset_list' } });
  check(list, { 'list 200': r => r.status === 200 });

  if (Math.random() < 0.5) { // 50% crean
    const payload = {
      name: `Spike_${Date.now()}_${Math.random().toString(16).slice(2,8)}`,
      username: USER,
      description: 'Spike test dataset',
      isDataset: 'S',
      contentType: '0'
    };
    const create = http.post(`${BASE_URL}/api/Dataset`, JSON.stringify(payload), {
      headers: { 'Content-Type': 'application/json' },
      tags: { endpoint: 'dataset_create' }
    });
    check(create, { 'create ok': r => r.status === 201 || r.status === 200 });
  }
  sleep(0.1);
}
