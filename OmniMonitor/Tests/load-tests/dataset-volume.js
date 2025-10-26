import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend } from 'k6/metrics';

export const options = {
  scenarios: {
    read_volume: {
      executor: 'ramping-arrival-rate',
      startRate: 10, // requests por segundo inicial
      timeUnit: '1s',
      preAllocatedVUs: 20,
      maxVUs: 100,
      stages: [
        { duration: '1m', target: 30 },
        { duration: '2m', target: 60 },
        { duration: '1m', target: 80 },
        { duration: '1m', target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.03'],
    http_req_duration: ['p(95)<1200'],
    'dataset_list_duration{endpoint:dataset_list}': ['p(95)<1000']
  },
  tags: { test_type: 'volume', domain: 'datasets' }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.LOGIN_USER || 'admin';
const PASS = __ENV.LOGIN_PASS || 'Secret123';

const listTrend = new Trend('dataset_list_duration');

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
  // Lectura intensiva de listado (sin creación para no contaminar volumen durante la medición)
  const res = http.get(`${BASE_URL}/api/Dataset/user?token=${data.token}`, { tags: { endpoint: 'dataset_list' } });
  listTrend.add(res.timings.duration);
  check(res, {
    'list 200': r => r.status === 200,
    'list array': r => Array.isArray(r.json())
  });

  // Acceso puntual a un dataset aleatorio si hay muchos
  const arr = res.json();
  if (Array.isArray(arr) && arr.length > 0) {
    const random = arr[Math.floor(Math.random() * arr.length)];
    if (random && random.id) {
      const one = http.get(`${BASE_URL}/api/Dataset/GetDataset?datasetId=${random.id}&token=${data.token}`, { tags: { endpoint: 'dataset_get' } });
      check(one, { 'get 200/404': r => r.status === 200 || r.status === 404 });
    }
  }

  // Pequeña pausa para permitir reutilización eficiente
  sleep(0.1);
}
