import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  duration: '20s',
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<600'],
    checks: ['rate>0.95']
  },
  tags: { test_type: 'smoke', domain: 'datasets' }
};

const BASE_URL = 'https://web-smartplatform-dev-c0cdendhffa2ghf2.mexicocentral-01.azurewebsites.net/';
const USER = 'admin';
const PASS = 'admin';

function login() {
  if (__ENV.TOKEN) {
    return __ENV.TOKEN; // reutilizar token provisto
  }
  const res = http.post(`${BASE_URL}/api/Auth/login`, JSON.stringify({ username: USER, password: PASS }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'auth_login' }
  });
  check(res, { 'login status 200': r => r.status === 200 });
  return res.json('token');
}

export function setup() {
  const token = login();
  return { token };
}

export default function (data) {
  // Listar datasets IM
  const listRes = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`, { tags: { endpoint: 'dataset_list' } });
  check(listRes, {
    'list status 200': r => r.status === 200,
    'list array': r => Array.isArray(r.json())
  });

  sleep(1);
}
