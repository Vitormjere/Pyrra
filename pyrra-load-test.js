import http from 'k6/http';
import { check, group } from 'k6';

// -----------------------------------------------------------------------------
// Teste de carga k6 — fluxo "usuario abre o app e ve o resumo do dia" (Pyrra)
//
// Como rodar (vus/duration NAO estao fixos no codigo, ajuste via CLI):
//   k6 run --vus 5  --duration 30s pyrra-load-test.js
//   k6 run --vus 10 --duration 30s pyrra-load-test.js
//   k6 run --vus 20 --duration 30s pyrra-load-test.js
//   k6 run --vus 30 --duration 30s pyrra-load-test.js
// -----------------------------------------------------------------------------

const BASE_URL = 'https://pyrra-api-evgpdqgke4agctct.brazilsouth-01.azurewebsites.net';

const CREDENTIALS = {
  email: 'teste@k6.com',
  password: 'Teste123@',
};

// So thresholds ficam no options — vus e duration vem do --vus / --duration.
export const options = {
  thresholds: {
    // 95% das requisicoes abaixo de 800ms.
    http_req_duration: ['p(95)<800'],
    // Menos de 1% de requisicoes com falha.
    http_req_failed: ['rate<0.01'],
    // Todos os checks (status 200) devem passar.
    checks: ['rate>0.99'],
  },
};

// setup() roda UMA vez antes do teste: faz login e devolve o token pra todas as VUs.
export function setup() {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify(CREDENTIALS),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'login' } },
  );

  const ok = check(res, {
    'login status 200': (r) => r.status === 200,
    'login retornou token': (r) => !!(r.json() && r.json().token),
  });

  if (!ok) {
    throw new Error(
      `Login falhou (status ${res.status}). Body: ${res.body}`,
    );
  }

  return { token: res.json().token };
}

// Cada iteracao de cada VU dispara as 4 leituras do resumo do dia.
export default function (data) {
  const params = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      'Content-Type': 'application/json',
    },
  };

  group('resumo-do-dia', () => {
    // As 4 leituras podem ir em lote (paralelo), como o app faria ao abrir.
    const responses = http.batch([
      ['GET', `${BASE_URL}/api/focos`,       null, { ...params, tags: { name: 'focos' } }],
      ['GET', `${BASE_URL}/api/focos/score`,  null, { ...params, tags: { name: 'focos-score' } }],
      ['GET', `${BASE_URL}/api/streak`,       null, { ...params, tags: { name: 'streak' } }],
      ['GET', `${BASE_URL}/api/tarefas`,      null, { ...params, tags: { name: 'tarefas' } }],
    ]);

    check(responses[0], { 'GET /api/focos -> 200':       (r) => r.status === 200 });
    check(responses[1], { 'GET /api/focos/score -> 200': (r) => r.status === 200 });
    check(responses[2], { 'GET /api/streak -> 200':      (r) => r.status === 200 });
    check(responses[3], { 'GET /api/tarefas -> 200':     (r) => r.status === 200 });
  });
}
