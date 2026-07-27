# 🔥 Pyrra

**Duolingo para a vida real.** Um app de hábitos gamificado que ajuda você a construir consistência em foco diário, treino, nutrição, finanças e reflexão. Tudo em um só lugar, com streaks, metas e um assistente de IA integrado.

🔗 **[pyrra.com.br](https://pyrra.com.br)**

---

## O que é o Pyrra

O Pyrra nasceu da ideia de que manter hábitos bons é difícil sozinho — mas fica mais fácil quando você tem um sistema de acompanhamento visual, gamificado e com feedback constante. Inspirado no modelo de streaks e progressão do Duolingo, o app une várias áreas da vida pessoal num único painel diário:

- 🔥 **Foco Diário** - hábitos e tarefas prioritárias, com streak e sistema de freeze
- 💪 **Treino** - planejamento semanal de academia e corrida
- 🍎 **Nutrição** - plano alimentar semanal
- 💰 **Finanças** - controle manual de saldo, com histórico visual
- 📓 **Diário** - reflexões e planejamento pessoal
- 🤖 **Zelo** - assistente de IA integrado, que responde dúvidas com contexto do seu progresso

## Stack técnica

**Backend**
- ASP.NET Core 9 · Clean Architecture (Domain / Application / Infrastructure / Api)
- Entity Framework Core · Azure SQL Database (Serverless)
- Autenticação JWT
- Integração com a API da Anthropic (Claude Haiku) para o assistente Zelo

**Frontend**
- React 19 · TypeScript · Vite
- Tailwind CSS v4
- Recharts (visualização de dados financeiros)

**Infraestrutura**
- Deploy: Azure App Service + Vercel
- CI/CD: GitHub Actions
- DNS: Cloudflare

## Como rodar localmente

### Pré-requisitos
- .NET 9 SDK
- Node.js 20+
- SQL Server LocalDB (ou Azure SQL, se preferir)

### Backend
```bash
cd Pyrra.Api
dotnet user-secrets set "Jwt:Key" "<sua-chave-secreta>"
dotnet run
```

### Frontend
```bash
cd Pyrra.Web
npm install
npm run dev
```

O frontend espera a API rodando em `https://localhost:7294` (configurável via variável de ambiente).

## Roadmap

- [ ] **Comunidade** - adicionar amigos, ranking, times competindo, desafios com prova por foto, conquistas de perfil
- [ ] **Zelo conversacional** - assistente sugerindo treino e dieta com base em perguntas guiadas, preenchendo as abas automaticamente
- [ ] **Validação de email** no cadastro
- [ ] Melhorias contínuas de layout e UX
- [ ] Lançamento como app (PWA)

## Sobre o projeto

Pyrra é um projeto pessoal de portfólio, desenvolvido por [Vitor Miranda Jeremias](https://github.com/Vitormjere), estudante de Engenharia de Software na PUCPR.
