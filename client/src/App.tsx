import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuth0 } from '@auth0/auth0-react'
import { setupAxiosInterceptor } from './services/api'
import AdminHome from './pages/AdminHome'
import HostGame from './pages/HostGame'
import TeamGame from './pages/TeamGame'
import Leaderboard from './pages/Leaderboard'

function App() {
  const { isAuthenticated, isLoading, loginWithRedirect, getAccessTokenSilently } = useAuth0()

  useEffect(() => {
    if (isAuthenticated) {
      setupAxiosInterceptor(() =>
        getAccessTokenSilently({ authorizationParams: { audience: 'https://trivia-v2' } })
      )
    }
  }, [isAuthenticated, getAccessTokenSilently])

  if (isLoading) {
    return <div className="flex items-center justify-center min-h-screen">Loading...</div>
  }

  if (!isAuthenticated) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-100">
        <div className="bg-white p-8 rounded shadow text-center">
          <h1 className="text-2xl font-bold mb-4">Trivia v2</h1>
          <button
            onClick={() => loginWithRedirect({ appState: { returnTo: window.location.pathname } })}
            className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
          >
            Log In
          </button>
        </div>
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<AdminHome />} />
        <Route path="/host/:gameId" element={<HostGame />} />
        <Route path="/game/:gameId" element={<TeamGame />} />
        <Route path="/leaderboard/:gameId" element={<Leaderboard />} />
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
