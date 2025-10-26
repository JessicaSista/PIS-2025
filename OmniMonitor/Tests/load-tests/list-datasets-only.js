import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

export const options = {
  vus: 10,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<700'],
    'list_only_duration{endpoint:dataset_list}': ['p(95)<650']
  },
  tags: { test_type: 'list_only', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';
const listTrend = new Trend('list_only_duration');

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
  const res = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`, { tags: { endpoint: 'dataset_list' } });
  listTrend.add(res.timings.duration);
  check(res, {
    'list 200': r => r.status === 200,
    'is array': r => Array.isArray(r.json())
  });
  sleep(0.5);
}
