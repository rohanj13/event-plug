import { useEffect, useState } from 'react'
import './App.css'

function App() {
  const [health, setHealth] = useState('Loading...')
  const [error, setError] = useState('')

  useEffect(() => {
    const loadHealth = async () => {
      try {
        const response = await fetch('/api/health')
        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const payload = await response.json()
        setHealth(payload.status ?? 'unknown')
      } catch (fetchError) {
        setError(fetchError instanceof Error ? fetchError.message : 'Unknown error')
      }
    }

    loadHealth()
  }, [])

  return (
    <main className="app">
      <h1>Event Plug</h1>
      <p>React frontend connected to a .NET API.</p>
      <div className="status">
        <strong>API health:</strong> {health}
      </div>
      {error && <div className="error">{error}</div>}
    </main>
  )
}

export default App
