import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth0 } from '@auth0/auth0-react'
import { api } from '../services/api'

interface Game {
  id: number
  title: string
  roomPin: string
  numberOfQuestions: number
  createdAt: string
  isEnded: boolean
}

export default function AdminHome() {
  const navigate = useNavigate()
  const { logout } = useAuth0()
  const [games, setGames] = useState<Game[]>([])
  const [showPast, setShowPast] = useState(false)

  const activeGames = games.filter(g => !g.isEnded)
  const pastGames = games.filter(g => g.isEnded)
  const [title, setTitle] = useState('')
  const [numberOfQuestions, setNumberOfQuestions] = useState(10)
  const [creating, setCreating] = useState(false)

  useEffect(() => {
    api.get('/api/games').then(res => setGames(res.data))
  }, [])

  async function createGame(e: React.FormEvent) {
    e.preventDefault()
    setCreating(true)
    try {
      const { data: game } = await api.post('/api/games', { title, numberOfQuestions })

      const triviaRes = await fetch(
        `https://opentdb.com/api.php?amount=${numberOfQuestions}&type=multiple`
      )
      const triviaData = await triviaRes.json()

      await api.post(`/api/questions/${game.id}`, triviaData.results.map((q: any) => ({
        category: q.category,
        question: q.question,
        correctAnswer: q.correct_answer,
        incorrectAnswers: q.incorrect_answers
      })))

      setGames(prev => [game, ...prev])
      setTitle('')
      setNumberOfQuestions(10)
    } finally {
      setCreating(false)
    }
  }

  async function deleteGame(id: number) {
    await api.delete(`/api/games/${id}`)
    setGames(prev => prev.filter(g => g.id !== id))
  }

  return (
    <div className="max-w-3xl mx-auto p-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">My Games</h1>
        <button
          onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
          className="text-sm text-gray-500 hover:text-gray-700"
        >
          Sign out
        </button>
      </div>

      <form onSubmit={createGame} className="bg-white rounded shadow p-6 mb-8 flex gap-4 items-end">
        <div className="flex-1">
          <label className="block text-sm font-medium mb-1">Title</label>
          <input
            className="w-full border rounded px-3 py-2"
            value={title}
            onChange={e => setTitle(e.target.value)}
            required
          />
        </div>
        <div className="w-32">
          <label className="block text-sm font-medium mb-1">Questions</label>
          <input
            type="number"
            min={1}
            max={50}
            className="w-full border rounded px-3 py-2"
            value={numberOfQuestions}
            onChange={e => setNumberOfQuestions(Number(e.target.value))}
          />
        </div>
        <button
          type="submit"
          disabled={creating}
          className="bg-blue-600 text-white px-5 py-2 rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {creating ? 'Creating...' : 'Create Game'}
        </button>
      </form>

      <div className="space-y-3">
        {activeGames.map(game => (
          <div key={game.id} className="bg-white rounded shadow p-4 flex items-center justify-between">
            <div>
              <p className="font-semibold">{game.title}</p>
              <p className="text-sm text-gray-500">PIN: {game.roomPin} · {game.numberOfQuestions} questions</p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => navigate(`/host/${game.id}`)}
                className="text-blue-600 hover:text-blue-800 text-sm font-medium"
              >
                Launch
              </button>
              <button
                onClick={() => deleteGame(game.id)}
                className="text-red-500 hover:text-red-700 text-sm"
              >
                Delete
              </button>
            </div>
          </div>
        ))}
        {activeGames.length === 0 && (
          <p className="text-gray-500 text-center py-8">No active games. Create one above.</p>
        )}
      </div>

      {pastGames.length > 0 && (
        <div className="mt-8">
          <button
            onClick={() => setShowPast(p => !p)}
            className="text-sm text-gray-500 hover:text-gray-700 mb-3 flex items-center gap-1"
          >
            <span>{showPast ? '▾' : '▸'}</span>
            Past Games ({pastGames.length})
          </button>
          {showPast && (
            <div className="space-y-3">
              {pastGames.map(game => (
                <div key={game.id} className="bg-gray-50 rounded shadow p-4 flex items-center justify-between opacity-75">
                  <div>
                    <p className="font-semibold">{game.title}</p>
                    <p className="text-sm text-gray-500">{game.numberOfQuestions} questions</p>
                  </div>
                  <div className="flex gap-3">
                    <button
                      onClick={() => navigate(`/leaderboard/${game.id}`)}
                      className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                    >
                      Results
                    </button>
                    <button
                      onClick={() => deleteGame(game.id)}
                      className="text-red-500 hover:text-red-700 text-sm"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
