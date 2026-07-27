// Lista curada de fusos IANA para o <select> de Configurações — não a lista completa (300+),
// que exigiria busca para ser usável. Cobre os fusos do Brasil (o grosso do público) e alguns
// globais comuns. O backend valida com TimeZoneInfo.TryFindSystemTimeZoneById, então qualquer
// IANA válido é aceito mesmo que não apareça aqui — a lista só limita as OPÇÕES do select.
export const TIMEZONE_OPTIONS: readonly { value: string; label: string }[] = [
  { value: 'America/Noronha', label: 'Fernando de Noronha (UTC-2)' },
  { value: 'America/Sao_Paulo', label: 'Brasília, São Paulo (UTC-3)' },
  { value: 'America/Manaus', label: 'Manaus (UTC-4)' },
  { value: 'America/Rio_Branco', label: 'Rio Branco (UTC-5)' },
  { value: 'America/New_York', label: 'Nova York (UTC-5)' },
  { value: 'America/Los_Angeles', label: 'Los Angeles (UTC-8)' },
  { value: 'Europe/Lisbon', label: 'Lisboa (UTC+0)' },
  { value: 'Europe/London', label: 'Londres (UTC+0)' },
  { value: 'Europe/Madrid', label: 'Madri (UTC+1)' },
  { value: 'UTC', label: 'UTC' },
]
