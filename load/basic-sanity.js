import http from 'k6/http';
import { check, sleep } from 'k6';

// Basic sanity load for a single-user personal tool — catches regressions/N+1s,
// not a capacity benchmark. Bump VUs/duration if this ever needs to mean more.
export const options = {
  vus: 10,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TOKEN = __ENV.TOKEN;

export default function () {
  const healthRes = http.get(`${BASE_URL}/health`);
  check(healthRes, { 'health status 200': (r) => r.status === 200 });

  const authHeaders = { headers: { Authorization: `Bearer ${TOKEN}` } };

  const portfolioRes = http.get(`${BASE_URL}/api/v1/portfolios`, authHeaders);
  check(portfolioRes, { 'portfolio status 200': (r) => r.status === 200 });

  const transactionsRes = http.get(`${BASE_URL}/api/v1/transactions`, authHeaders);
  check(transactionsRes, { 'transactions status 200': (r) => r.status === 200 });

  sleep(1);
}
