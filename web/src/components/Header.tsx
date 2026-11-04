import { Link, useNavigate } from 'react-router'
import { isAdmin, useAuth } from '../auth/context.ts'

/** Top bar: the signed-in person, the admin link for admins, sign-out (spec 0002, User experience). */
export function Header() {
  const { account, signOut } = useAuth()
  const navigate = useNavigate()

  async function handleSignOut() {
    await signOut()
    void navigate('/giris')
  }

  return (
    <header>
      <h1>
        <Link to="/">Polsan Auction</Link>
      </h1>
      <nav aria-label="Hesap">
        {account === null && (
          <>
            <Link to="/giris">Giriş yap</Link> <Link to="/kayit">Kayıt ol</Link>
          </>
        )}
        {account && (
          <>
            <span>
              {account.firstName} {account.lastName}
            </span>{' '}
            {isAdmin(account) && <Link to="/yonetim">Yönetim</Link>}{' '}
            <button type="button" onClick={() => void handleSignOut()}>
              Çıkış
            </button>
          </>
        )}
      </nav>
    </header>
  )
}
