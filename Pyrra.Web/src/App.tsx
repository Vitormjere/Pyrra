import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/AppLayout'
import ProtectedRoute from './components/ProtectedRoute'
import { AuthProvider } from './contexts/AuthContext'
import Cadastro from './pages/Cadastro'
import Financas from './pages/Financas'
import Hoje from './pages/Hoje'
import Login from './pages/Login'
import Nutricao from './pages/Nutricao'
import Perfil from './pages/Perfil'
import Tarefas from './pages/Tarefas'
import Treino from './pages/Treino'

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

          {/* Duas rotas de layout aninhadas, e a ordem importa: o guard vem por
              FORA da casca. Assim, para quem não tem sessão, o AppLayout nunca
              chega a montar — sem isso a navegação inferior apareceria por um
              instante antes do redirect, entregando a estrutura do app a quem
              não está logado. O ProtectedRoute também segura a renderização
              enquanto a sessão está sendo verificada, então nem a casca pisca. */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/hoje" element={<Hoje />} />
              <Route path="/treino" element={<Treino />} />
              <Route path="/tarefas" element={<Tarefas />} />
              <Route path="/financas" element={<Financas />} />
              <Route path="/nutricao" element={<Nutricao />} />
              <Route path="/perfil" element={<Perfil />} />
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
