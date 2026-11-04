import { Route, Routes } from 'react-router'
import { AuthProvider } from './auth/AuthContext.tsx'
import { Header } from './components/Header.tsx'
import { AdminPage } from './pages/AdminPage.tsx'
import { ConfirmEmailPage } from './pages/ConfirmEmailPage.tsx'
import { HomePage } from './pages/HomePage.tsx'
import { LoginPage } from './pages/LoginPage.tsx'
import { RegisterPage } from './pages/RegisterPage.tsx'

/** Layout and routes. User-facing paths are Turkish; API paths stay English (docs/conventions.md). */
export default function App() {
  return (
    <AuthProvider>
      <Header />
      <main>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/kayit" element={<RegisterPage />} />
          <Route path="/giris" element={<LoginPage />} />
          <Route path="/dogrula" element={<ConfirmEmailPage />} />
          <Route path="/yonetim" element={<AdminPage />} />
        </Routes>
      </main>
    </AuthProvider>
  )
}
