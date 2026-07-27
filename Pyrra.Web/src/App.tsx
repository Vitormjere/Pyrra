import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import AppLayout from './components/AppLayout'
import ProtectedRoute from './components/ProtectedRoute'
import RequireOnboarding from './components/RequireOnboarding'
import RequireUsername from './components/RequireUsername'
import { AuthProvider } from './contexts/AuthContext'
import { FriendRequestsProvider } from './contexts/FriendRequestsProvider'
import Agenda from './pages/Agenda'
import Amigos from './pages/Amigos'
import Cadastro from './pages/Cadastro'
import Configuracoes from './pages/Configuracoes'
import Convite from './pages/Convite'
import Diario from './pages/Diario'
import EscolherUsername from './pages/EscolherUsername'
import Financas from './pages/Financas'
import Hoje from './pages/Hoje'
import Login from './pages/Login'
import Nutricao from './pages/Nutricao'
import Onboarding from './pages/Onboarding'
import Perfil from './pages/Perfil'
import Tarefas from './pages/Tarefas'
import Termos from './pages/Termos'
import Treino from './pages/Treino'
import Zelo from './pages/Zelo'

// AuthProvider fica DENTRO do BrowserRouter para poder usar hooks do router
// (useNavigate, useLocation) quando o fluxo de autenticação crescer.
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
                {/* Provider da contagem de pedidos ACIMA do AppLayout, para o badge do menu e a
                    tela de Amigos lerem a mesma contagem — e onde o convite pendente é consumido. */}
                <Route element={<FriendRequestsProvider><Outlet /></FriendRequestsProvider>}>
                  <Route element={<AppLayout />}>
                    <Route path="/hoje" element={<Hoje />} />
                    <Route path="/agenda" element={<Agenda />} />
                    <Route path="/treino" element={<Treino />} />
                    <Route path="/tarefas" element={<Tarefas />} />
                    <Route path="/financas" element={<Financas />} />
                    <Route path="/nutricao" element={<Nutricao />} />
                    <Route path="/zelo" element={<Zelo />} />
                    <Route path="/diario" element={<Diario />} />
                    <Route path="/amigos" element={<Amigos />} />
                    <Route path="/perfil" element={<Perfil />} />
                    {/* Não entra no menu principal (ALL_SECTIONS): é destino ocasional, alcançado
                        pelo ícone de engrenagem no Perfil, não uma seção de uso diário. */}
                    <Route path="/configuracoes" element={<Configuracoes />} />
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
