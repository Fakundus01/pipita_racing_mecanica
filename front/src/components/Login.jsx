import { useState } from 'react'

function Login({ onSubmit, error, loading }) {
  const [form, setForm] = useState({ username: '', password: '' })

  const handleChange = (event) => {
    const { name, value } = event.target
    setForm((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    onSubmit(form)
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="auth-header">
          <span className="brand-icon">🏁</span>
          <div>
            <h1>Pipita Garage</h1>
            <p>Ingresa como administrador para gestionar ventas.</p>
          </div>
        </div>
        <form className="auth-form" onSubmit={handleSubmit}>
          <label>
            Usuario
            <input
              name="username"
              value={form.username}
              onChange={handleChange}
              placeholder="admin"
              autoComplete="username"
              required
            />
          </label>
          <label>
            Contraseña
            <input
              name="password"
              type="password"
              value={form.password}
              onChange={handleChange}
              placeholder="••••••••"
              autoComplete="current-password"
              required
            />
          </label>
          {error ? <p className="auth-error">{error}</p> : null}
          <button className="primary" type="submit" disabled={loading}>
            {loading ? 'Validando...' : 'Ingresar'}
          </button>
        </form>
      </div>
    </div>
  )
}

export default Login