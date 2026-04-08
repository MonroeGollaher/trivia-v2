import { useEffect, useState } from 'react'
import axios from 'axios'

function App() {
  const [status, setStatus] = useState<string>('checking...')

  useEffect(() => {
    axios.get('/health')
      .then(res => setStatus(res.data.status))
      .catch(() => setStatus('unreachable'))
  }, [])

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100">
      <div className="bg-white p-8 rounded shadow text-center">
        <h1 className="text-2xl font-bold mb-4">Trivia v2</h1>
        <p className="text-gray-600">API status: <span className="font-mono font-semibold">{status}</span></p>
      </div>
    </div>
  )
}

export default App
