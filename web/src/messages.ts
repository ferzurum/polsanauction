import type { ApiProblem } from './api/client.ts'

/** Turkish texts for API error codes (docs/conventions.md: the web maps codes, the API never sends prose). */
const fieldMessages: Record<string, Record<string, string>> = {
  firstName: { required: 'Adınızı girin.', too_long: 'Ad en fazla 100 karakter olabilir.' },
  lastName: { required: 'Soyadınızı girin.', too_long: 'Soyad en fazla 100 karakter olabilir.' },
  email: {
    required: 'E-posta adresinizi girin.',
    'email.invalid': 'Geçerli bir e-posta adresi girin.',
    'email.domain_not_allowed': 'Yalnızca kurumsal e-posta adresleriyle kayıt olunabilir.',
  },
  password: {
    required: 'Bir parola belirleyin.',
    too_short: 'Parola en az 10 karakter olmalı.',
    too_long: 'Parola en fazla 128 karakter olabilir.',
  },
  passwordConfirmation: { 'password.mismatch': 'Parolalar eşleşmiyor.' },
}

export const messages = {
  checkEmail:
    'E-postanızı kontrol edin. Hesabınızı etkinleştirmek için gönderdiğimiz bağlantıyı açın.',
  serverError: 'Bir hata oluştu. Lütfen daha sonra tekrar deneyin.',
  tooManyAttempts: 'Çok fazla deneme, lütfen sonra tekrar deneyin.',
  invalidCredentials: 'E-posta adresi veya parola hatalı.',
  emailNotConfirmed: 'E-posta adresiniz henüz doğrulanmadı.',
  confirmationSent: 'Yeni bir doğrulama bağlantısı gönderildi. E-postanızı kontrol edin.',
  confirming: 'E-posta adresiniz doğrulanıyor…',
  confirmed: 'E-postanız doğrulandı, giriş yapabilirsiniz.',
  confirmationInvalid: 'Bu bağlantı geçersiz, süresi dolmuş ya da daha önce kullanılmış.',
  noPermission: 'Bu sayfaya erişim yetkiniz yok.',
  adminPanel: 'Yönetim paneli',
}

export function fieldError(field: string, codes: string[] | undefined): string | undefined {
  const code = codes?.[0]
  return code === undefined ? undefined : (fieldMessages[field]?.[code] ?? messages.serverError)
}

/**
 * "… Kilit açılış saati: 14:35." — shown in Istanbul time (docs/conventions.md, Data rules). No suffix after the
 * time: Turkish suffixes follow the spoken number ("14:30'da", "14:35'te"), so a fixed one is often wrong.
 */
export function lockedMessage(lockedUntil: string | undefined): string {
  if (lockedUntil === undefined) {
    return 'Çok fazla hatalı deneme. Hesabınız geçici olarak kilitlendi.'
  }
  const time = new Intl.DateTimeFormat('tr-TR', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(lockedUntil))
  return `Çok fazla hatalı deneme. Hesabınız geçici olarak kilitlendi. Kilit açılış saati: ${time}.`
}

export function isRateLimited(problem: ApiProblem) {
  return problem.status === 429 || problem.code === 'rate_limited'
}
