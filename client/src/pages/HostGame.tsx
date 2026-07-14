import { useEffect, useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { api } from "../services/api";
import { socketService } from "../services/SocketService";

interface Question {
  id: number;
  text: string;
  category: string;
  answer: string;
}

interface Response {
  id: number;
  teamId: string;
  answer: string;
  wager: number;
  approved: boolean;
  team: { name: string; teamName: string | null };
}

interface Game {
  id: number;
  title: string;
  roomPin: string;
  activeQuestionIndex: number;
}

export default function HostGame() {
  const { gameId } = useParams<{ gameId: string }>();
  const navigate = useNavigate();
  const [game, setGame] = useState<Game | null>(null);
  const [questions, setQuestions] = useState<Question[]>([]);
  const [responses, setResponses] = useState<Response[]>([]);
  const activeQuestionIdRef = useRef<number | null>(null);

  const activeQuestion = questions[game?.activeQuestionIndex ?? 0];

  useEffect(() => {
    activeQuestionIdRef.current = activeQuestion?.id ?? null;
  }, [activeQuestion]);

  async function loadResponses(questionId: number) {
    const res = await api.get(`/api/responses/${questionId}`);
    setResponses(res.data);
  }

  useEffect(() => {
    if (!gameId) return;

    Promise.all([
      api.get(`/api/games/${gameId}`),
      api.get(`/api/questions/${gameId}`)
    ]).then(([gameRes, questionsRes]) => {
      setGame(gameRes.data);
      setQuestions(questionsRes.data);
      const idx = gameRes.data.activeQuestionIndex;
      const currentQuestion = questionsRes.data[idx];
      if (currentQuestion) loadResponses(currentQuestion.id);
    });

    socketService.connect().then(() => {
      console.log('[Host] Joining room:', gameId);
      socketService.joinAsHost(gameId);
      socketService.onOrderRanking((payload) => {
        console.log('[Host] orderRanking received, questionId:', activeQuestionIdRef.current, payload);
        if (activeQuestionIdRef.current) loadResponses(activeQuestionIdRef.current);
      });
    });

    return () => {
      socketService.leaveRoom(gameId);
      socketService.offOrderRanking();
      socketService.disconnect();
    };
  }, [gameId]);

  async function nextQuestion() {
    const res = await api.put(`/api/questions/${gameId}/next`);
    setGame(res.data);
    setResponses([]);
    socketService.nextQuestion(gameId!, res.data);
  }

  async function endGame() {
    await api.put(`/api/games/${gameId}/end`);
    socketService.endGame(gameId!, {});
    navigate(`/leaderboard/${gameId}`);
  }

  async function setApproval(responseId: number, approve: boolean) {
    const current = responses.find((r) => r.id === responseId);
    if (!current || current.approved === approve) return;
    const res = await api.put(`/api/responses/${responseId}/approval`);
    setResponses((prev) =>
      prev.map((r) => (r.id === responseId ? { ...r, approved: res.data.approved } : r))
    );
  }

  if (!game || !activeQuestion) {
    return <div className="p-8 text-gray-500">Loading...</div>;
  }

  const isLastQuestion = game.activeQuestionIndex >= questions.length - 1;

  return (
    <div className="max-w-3xl mx-auto p-8">
      <div className="flex items-center justify-between mb-2">
        <h1 className="text-2xl font-bold">{game.title}</h1>
        <span className="text-sm text-gray-500">PIN: {game.roomPin}</span>
      </div>
      <p className="text-sm text-gray-500 mb-6">
        Question {game.activeQuestionIndex + 1} of {questions.length}
      </p>

      <div className="bg-white rounded shadow p-6 mb-6">
        <p className="text-xs text-gray-400 uppercase mb-1">
          {activeQuestion.category}
        </p>
        <p
          className="text-lg font-medium"
          dangerouslySetInnerHTML={{ __html: activeQuestion.text }}
        />
        <p className="mt-3 text-green-600 font-semibold">
          Answer:{" "}
          <span dangerouslySetInnerHTML={{ __html: activeQuestion.answer }} />
        </p>
      </div>

      <div className="mb-6">
        <h2 className="font-semibold mb-3">Team Responses</h2>
        {responses.length === 0 ? (
          <p className="text-gray-400 text-sm">No responses yet.</p>
        ) : (
          <div className="space-y-2">
            {responses.map((r) => (
              <div
                key={r.id}
                className="bg-white rounded shadow p-4 flex items-center justify-between"
              >
                <div>
                  <p className="font-medium">
                    {r.team?.teamName ?? r.team?.name ?? r.teamId}
                  </p>
                  <p
                    className="text-sm text-gray-600"
                    dangerouslySetInnerHTML={{ __html: r.answer }}
                  />
                  <p className="text-xs text-gray-400">Wager: {r.wager}</p>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => setApproval(r.id, true)}
                    className={`px-3 py-1 rounded text-sm font-medium ${
                      r.approved
                        ? "bg-green-100 text-green-700"
                        : "bg-gray-100 text-gray-500 hover:bg-green-50 hover:text-green-600"
                    }`}
                  >
                    Approve
                  </button>
                  <button
                    onClick={() => setApproval(r.id, false)}
                    className={`px-3 py-1 rounded text-sm font-medium ${
                      !r.approved
                        ? "bg-red-100 text-red-700"
                        : "bg-gray-100 text-gray-500 hover:bg-red-50 hover:text-red-600"
                    }`}
                  >
                    Deny
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {isLastQuestion ? (
        <button
          onClick={endGame}
          className="bg-green-600 text-white px-6 py-2 rounded hover:bg-green-700"
        >
          End Game & See Results
        </button>
      ) : (
        <button
          onClick={nextQuestion}
          className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
        >
          Next Question
        </button>
      )}
    </div>
  );
}
