import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import ProtectedRoute from './components/ProtectedRoute'
import RequireAdmin from './components/RequireAdmin'
import RequireNotAdmin from './components/RequireNotAdmin'
import RequireOnboarding from './components/RequireOnboarding'
import RequireUsername from './components/RequireUsername'
import RootLayout from './components/RootLayout'
import { AuthProvider } from './contexts/AuthContext'
import { ChatConnectionProvider } from './contexts/ChatConnectionProvider'
import { ChatUnreadProvider } from './contexts/ChatUnreadProvider'
import { FriendRequestsProvider } from './contexts/FriendRequestsProvider'
import { TeamInvitesProvider } from './contexts/TeamInvitesProvider'
import Agenda from './pages/Agenda'
import AdminContas from './pages/Admin/Contas'
import AdminMensagens from './pages/Admin/Mensagens'
import AdminSolicitacoes from './pages/Admin/Solicitacoes'
import Amigos from './pages/Amigos'
import Cadastro from './pages/Cadastro'
import Configuracoes from './pages/Configuracoes'
import Convite from './pages/Convite'
import ConviteTime from './pages/ConviteTime'
import ConviteTorneio from './pages/ConviteTorneio'
import Diario from './pages/Diario'
import EscolherUsername from './pages/EscolherUsername'
import Financas from './pages/Financas'
import Hoje from './pages/Hoje'
import Login from './pages/Login'
import Nutricao from './pages/Nutricao'
import Onboarding from './pages/Onboarding'
import Perfil from './pages/Perfil'
import PerfilPublico from './pages/PerfilPublico'
import Suporte from './pages/Suporte'
import Tarefas from './pages/Tarefas'
import Termos from './pages/Termos'
import CriarTime from './pages/Times/Criar'
import TimeDetalhe from './pages/Times/Detalhe'
import Times from './pages/Times'
import CriarTorneio from './pages/Torneios/Criar'
import SolicitarTorneio from './pages/Torneios/Solicitar'
import TorneioDetalhe from './pages/Torneios/Detalhe'
import Torneios from './pages/Torneios'
import Treino from './pages/Treino'
import Zelo from './pages/Zelo'

// AuthProvider fica dentro do BrowserRouter pra poder usar hooks do router (useNavigate, useLocation) no futuro
function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Públicas: as únicas alcançáveis sem sessão. */}
          <Route path="/login" element={<Login />} />
          <Route path="/cadastro" element={<Cadastro />} />
          <Route path="/termos" element={<Termos />} />

          {/* Convite é público de propósito: quem abre deslogado precisa chegar aqui para o token
              ser guardado antes do redirect ao login. Logado, envia o pedido na hora. */}
          <Route path="/convite/:token" element={<Convite />} />

          {/* Mesmo raciocínio do convite de amizade, agora para o link de convite de time. */}
          <Route path="/times/convite/:token" element={<ConviteTime />} />

          {/* Convite de torneio: mesma posição pública (a tela lida com sessão sozinha), mas o
              desfecho é uma SOLICITAÇÃO de entrada (aprovação do dono do torneio), não entrada
              direta — por isso escolhe qual time próprio usar, em vez de agir sozinha. */}
          <Route path="/torneios/convite/:token" element={<ConviteTorneio />} />

          {/* Duas rotas de layout aninhadas, e a ordem importa: o guard vem por
              FORA da casca. Assim, para quem não tem sessão, o AppLayout nunca
              chega a montar — sem isso a navegação inferior apareceria por um
              instante antes do redirect, entregando a estrutura do app a quem
              não está logado. O ProtectedRoute também segura a renderização
              enquanto a sessão está sendo verificada, então nem a casca pisca. */}
          <Route element={<ProtectedRoute />}>
            {/* /onboarding vive dentro do guard de sessão, mas FORA do
                RequireOnboarding — senão a tela redirecionaria para si mesma. */}
            <Route path="/onboarding" element={<Onboarding />} />

            {/* Depois do onboarding vem o gate de username. A tela /username fica DENTRO do
                RequireOnboarding (onboarding já feito) e FORA do RequireUsername (senão a própria
                tela de escolha entraria em loop de redirect). */}
            <Route element={<RequireOnboarding />}>
              <Route path="/username" element={<EscolherUsername />} />

              <Route element={<RequireUsername />}>
                {/* Provider da contagem de pedidos ACIMA do layout, para o badge do menu e a
                    tela de Amigos lerem a mesma contagem — e onde o convite pendente é consumido. */}
                {/* TeamInvitesProvider aninhado ao FriendRequestsProvider: mesma posição acima do
                    layout, para o badge do menu e a tela de Times lerem a mesma contagem — e
                    onde o convite de time pendente é consumido. ChatConnectionProvider por cima
                    dos dois, mesma posição — dono da conexão SignalR compartilhada (Fase
                    Admin-4b); ChatUnreadProvider por cima dele, já que agora também escuta a
                    conexão para atualizar o badge de "Suporte"/"Mensagens" em tempo real. */}
                <Route element={<FriendRequestsProvider><TeamInvitesProvider><ChatConnectionProvider><ChatUnreadProvider><Outlet /></ChatUnreadProvider></ChatConnectionProvider></TeamInvitesProvider></FriendRequestsProvider>}>
                  {/* RootLayout escolhe AppLayout ou AdminLayout conforme IsAdmin (Fase Admin-1) —
                      monta uma vez por sessão, então nem ele nem os providers acima remontam ao
                      navegar entre seções, admin ou não. */}
                  <Route element={<RootLayout />}>
                    {/* Seções operacionais do dia a dia — fora do alcance de contas admin, que não
                        têm essas seções no menu (ver AdminLayout/RequireNotAdmin). Suporte entra
                        aqui: é o chat do JOGADOR com o admin — quem já é admin tem "Mensagens" no
                        próprio menu, não "Suporte". */}
                    <Route element={<RequireNotAdmin />}>
                      <Route path="/hoje" element={<Hoje />} />
                      <Route path="/agenda" element={<Agenda />} />
                      <Route path="/treino" element={<Treino />} />
                      <Route path="/tarefas" element={<Tarefas />} />
                      <Route path="/financas" element={<Financas />} />
                      <Route path="/nutricao" element={<Nutricao />} />
                      <Route path="/zelo" element={<Zelo />} />
                      <Route path="/diario" element={<Diario />} />
                      <Route path="/amigos" element={<Amigos />} />
                      <Route path="/suporte" element={<Suporte />} />
                    </Route>

                    {/* Compartilhadas entre app comum e admin — mesmas telas para os dois. */}
                    <Route path="/times" element={<Times />} />
                    <Route path="/times/novo" element={<CriarTime />} />
                    <Route path="/times/:id" element={<TimeDetalhe />} />
                    <Route path="/torneios" element={<Torneios />} />
                    <Route path="/torneios/solicitar" element={<SolicitarTorneio />} />
                    <Route path="/torneios/criar" element={<CriarTorneio />} />
                    <Route path="/torneios/:id" element={<TorneioDetalhe />} />
                    <Route path="/perfil" element={<Perfil />} />
                    {/* Perfil de TERCEIRO, por username — precisa vir depois de /perfil na
                        árvore para não colidir com ela (react-router já resolveria certo mesmo
                        com a ordem trocada, mas a leitura fica mais clara assim). */}
                    <Route path="/perfil/:username" element={<PerfilPublico />} />
                    {/* Não entra no menu principal do app comum: é destino ocasional, alcançado
                        pelo ícone de engrenagem no Perfil, não uma seção de uso diário. No admin,
                        entra no menu como qualquer outra seção. */}
                    <Route path="/configuracoes" element={<Configuracoes />} />

                    {/* Administrativas — fora do alcance de contas comuns (Fase Admin-1: só
                        placeholders, funcionalidade real chega nas próximas etapas). */}
                    <Route element={<RequireAdmin />}>
                      <Route path="/admin/contas" element={<AdminContas />} />
                      <Route path="/admin/solicitacoes" element={<AdminSolicitacoes />} />
                      <Route path="/admin/mensagens" element={<AdminMensagens />} />
                    </Route>
                  </Route>
                </Route>
              </Route>
            </Route>
          </Route>

          {/* "/" e rotas desconhecidas caem em /hoje, que por ser protegida
              devolve ao login quem não tem sessão. */}
          <Route path="/" element={<Navigate to="/hoje" replace />} />
          <Route path="*" element={<Navigate to="/hoje" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
