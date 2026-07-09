import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useAuth0 } from '@auth0/auth0-react'
import { api } from '../services/api'
import { socketService } from '../services/SocketService'

interface Question {
  id: number
  text: string
  category: string
}

interface Game {
  id: number
  title: string
  roomPin: string
  activeQuestionIndex: number
}

type Stage = 'loading' | 'joining' | 'playing'

export default function TeamGame() {
  const { gameId } = useParams<{ gameId: string }>()
  const { user } = useAuth0()
  const [stage, setStage] = useState<Stage>('loading')
  const [game, setGame] = useState<Game | null>(null)
  const [questions, setQuestions] = useState<Question[]>([])
  const [teamName, setTeamName] = useState('')
  const [answer, setAnswer] = useState('')
  const [wager, setWager] = useState(1)
  const [submitted, setSubmitted] = useState(false)

  const activeQuestion = questions[game?.activeQuestionIndex ?? 0]

  useEffect(() => {
    if (!gameId || !user) return

    Promise.all([
      api.get(`/api/games/${gameId}`),
      api.get(`/api/questions/${gameId}`),
      api.get('/api/profiles/me').catch(() => null)
    ]).then(async ([gameRes, questionsRes, profileRes]) => {
      setGame(gameRes.data)
      setQuestions(questionsRes.data)

      if (!profileRes) {
        await api.post('/api/profiles/me', {
          email: user.email ?? '',
          name: user.name ?? '',
          picture: user.picture ?? null
        })
      }

      const profile = profileRes?.data
      if (profile?.currentGameId === gameRes.data.id && profile?.teamName) {
        setStage('playing')
        connectSocket(gameId)
      } else {
        setStage('joining')
      }
    })
  }, [gameId, user])

  function connectSocket(gid: string) {
    socketService.connect().then(() => {
      socketService.joinRoom(gid)
      socketService.onNextQuestion((payload: any) => {
        setGame(payload)
        setAnswer('')
        setWager(1)
        setSubmitted(false)
      })
    })
  }

  async function joinGame(e: React.FormEvent) {
    e.preventDefault()
    if (!game) return
    await api.put(`/api/profiles/joingame/${game.roomPin}`, { teamName })
    setStage('playing')
    connectSocket(gameId!)
  }

  async function submitAnswer(e: React.FormEvent) {
    e.preventDefault()
    if (!activeQuestion) return
    await api.post(`/api/responses/${activeQuestion.id}`, { answer, wager })
    setSubmitted(true)
  }

  if (stage === 'loading') {
    return <div className="p-8 text-gray-500">Loading...</div>
  }

  if (stage === 'joining') {
    return (
      <div className="max-w-sm mx-auto p-8">
        <h1 className="text-2xl font-bold mb-6">{game?.title}</h1>
        <form onSubmit={joinGame} className="bg-white rounded shadow p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Team Name</label>
            <input
              className="w-full border rounded px-3 py-2"
              value={teamName}
              onChange={e => setTeamName(e.target.value)}
              placeholder="Enter your team name"
              required
            />
          </div>
          <button
            type="submit"
            className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700"
          >
            Join Game
          </button>
        </form>
      </div>
    )
  }

  if (!activeQuestion) {
    return <div className="p-8 text-gray-500">Waiting for game to start...</div>
  }

  return (
    <div className="max-w-lg mx-auto p-8">
      <h1 className="text-2xl font-bold mb-1">{game?.title}</h1>
      <p className="text-sm text-gray-500 mb-6">
        Question {(game?.activeQuestionIndex ?? 0) + 1}
      </p>

      <div className="bg-white rounded shadow p-6 mb-6">
        <p className="text-xs text-gray-400 uppercase mb-1">{activeQuestion.category}</p>
        <p className="text-lg font-medium"
          dangerouslySetInnerHTML={{ __html: activeQuestion.text }}
        />
      </div>

      {submitted ? (
        <div className="bg-green-50 border border-green-200 rounded p-6 text-center">
          <p className="text-green-700 font-medium">Answer submitted!</p>
          <p className="text-sm text-gray-500 mt-1">Waiting for the host to advance...</p>
        </div>
      ) : (
        <form onSubmit={submitAnswer} className="bg-white rounded shadow p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Your Answer</label>
            <input
              className="w-full border rounded px-3 py-2"
              value={answer}
              onChange={e => setAnswer(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Wager</label>
            <input
              type="number"
              min={1}
              className="w-full border rounded px-3 py-2"
              value={wager}
              onChange={e => setWager(Number(e.target.value))}
            />
          </div>
          <button
            type="submit"
            className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700"
          >
            Submit Answer
          </button>
        </form>
      )}
    </div>
  )
}
