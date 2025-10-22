import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },
        { duration: '2m', target: 50 },
        { duration: '2m', target: 80 },
        { duration: '1m', target: 100 },
        { duration: '1m', target: 0 }
      ],
      gracefulRampDown: '30s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000']
  },
  tags: { test_type: 'stress', domain: 'datasets' }
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
  // Mezcla pequeña de list + create para presión
  const list = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`, { tags: { endpoint: 'dataset_list' } });
  check(list, { 'list 200': r => r.status === 200 });

  if (Math.random() < 0.3) { // 30% de iteraciones crean dataset
    const payload = {
      name: `Stress_${Date.now()}_${Math.random().toString(16).slice(2,8)}`,
      username: USER,
      description: 'Stress test dataset',
      isDataset: 'S',
      contentType: '0'
    };
    const create = http.post(`${BASE_URL}/api/Dataset`, JSON.stringify(payload), {
      headers: { 'Content-Type': 'application/json' },
      tags: { endpoint: 'dataset_create' }
    });
    check(create, { 'create ok': r => r.status === 201 || r.status === 200 });
  }

  sleep(0.2);
}
