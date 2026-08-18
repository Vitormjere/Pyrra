self.addEventListener('install', () => self.skipWaiting())
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()))
// Sem cache proposital: fetch passa direto pra rede, sempre dado fresco.
self.addEventListener('fetch', () => {})
