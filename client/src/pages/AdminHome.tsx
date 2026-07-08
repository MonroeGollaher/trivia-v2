import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../services/api'

interface Game {
  id: number
  title: string
  roomPin: string
  numberOfQuestions: number
  createdAt: string
}

export default function AdminHome() {
  const navigate = useNavigate()
  const [games, setGames] = useState<Game[]>([])
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
      <h1 className="text-2xl font-bold mb-6">My Games</h1>

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
        {games.map(game => (
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
        {games.length === 0 && (
          <p className="text-gray-500 text-center py-8">No games yet. Create one above.</p>
        )}
      </div>
    </div>
  )
}
