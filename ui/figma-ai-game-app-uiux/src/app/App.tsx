import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "motion/react";
import { AICharacter } from "./components/AICharacter";
import { CoinDisplay } from "./components/CoinDisplay";
import { UpgradeCard } from "./components/UpgradeCard";
import { MissionCard } from "./components/MissionCard";
import { TabButton } from "./components/TabButton";
import {
  Home,
  ShoppingBag,
  Trophy,
  Zap,
  Brain,
  Cpu,
  Gauge,
  Rocket,
  TrendingUp,
} from "lucide-react";

interface Upgrade {
  id: string;
  icon: any;
  title: string;
  description: string;
  level: number;
  baseCost: number;
  effect: number;
  color: string;
}

interface Mission {
  id: string;
  title: string;
  progress: number;
  target: number;
  reward: number;
  completed: boolean;
}

type Mood = "happy" | "excited" | "thinking";

export default function App() {
  const [coins, setCoins] = useState(0);
  const [level, setLevel] = useState(1);
  const [exp, setExp] = useState(0);
  const [mood, setMood] = useState<Mood>("happy");
  const [activeTab, setActiveTab] = useState<"home" | "shop" | "mission">("home");
  const [totalTaps, setTotalTaps] = useState(0);
  const [tapStreak, setTapStreak] = useState(0);
  const [lastTapTime, setLastTapTime] = useState(0);

  const [upgrades, setUpgrades] = useState<Upgrade[]>([
    {
      id: "tap",
      icon: Zap,
      title: "탭 파워",
      description: "탭당 획득 코인 증가",
      level: 1,
      baseCost: 10,
      effect: 1,
      color: "#eab308",
    },
    {
      id: "auto",
      icon: TrendingUp,
      title: "자동 수입",
      description: "초당 자동으로 코인 획득",
      level: 0,
      baseCost: 50,
      effect: 0,
      color: "#10b981",
    },
    {
      id: "multiplier",
      icon: Rocket,
      title: "경험치 배율",
      description: "획득 경험치 증가",
      level: 1,
      baseCost: 100,
      effect: 1,
      color: "#f59e0b",
    },
    {
      id: "intelligence",
      icon: Brain,
      title: "지능 향상",
      description: "AI 학습 속도 증가",
      level: 0,
      baseCost: 200,
      effect: 0,
      color: "#8b5cf6",
    },
  ]);

  const [missions, setMissions] = useState<Mission[]>([
    {
      id: "tap100",
      title: "AI를 100번 탭하기",
      progress: 0,
      target: 100,
      reward: 50,
      completed: false,
    },
    {
      id: "coins500",
      title: "코인 500개 모으기",
      progress: 0,
      target: 500,
      reward: 100,
      completed: false,
    },
    {
      id: "level5",
      title: "레벨 5 달성하기",
      progress: 0,
      target: 5,
      reward: 200,
      completed: false,
    },
    {
      id: "upgrade3",
      title: "업그레이드 3번 구매하기",
      progress: 0,
      target: 3,
      reward: 150,
      completed: false,
    },
  ]);

  // 레벨 계산
  const expToNextLevel = level * 50;
  const autoIncomePerSecond = upgrades.find((u) => u.id === "auto")?.effect || 0;
  const tapPower = upgrades.find((u) => u.id === "tap")?.effect || 1;
  const expMultiplier = upgrades.find((u) => u.id === "multiplier")?.effect || 1;

  // 자동 수입
  useEffect(() => {
    if (autoIncomePerSecond > 0) {
      const interval = setInterval(() => {
        setCoins((prev) => prev + autoIncomePerSecond);
        setMissions((prev) =>
          prev.map((m) =>
            m.id === "coins500" && !m.completed
              ? { ...m, progress: Math.min(m.progress + autoIncomePerSecond, m.target) }
              : m
          )
        );
      }, 1000);
      return () => clearInterval(interval);
    }
  }, [autoIncomePerSecond]);

  // 레벨업 체크
  useEffect(() => {
    if (exp >= expToNextLevel) {
      setExp(exp - expToNextLevel);
      setLevel((prev) => prev + 1);
      setMood("excited");
      setTimeout(() => setMood("happy"), 2000);
      
      // 레벨 미션 진행도 업데이트
      setMissions((prev) =>
        prev.map((m) =>
          m.id === "level5" && !m.completed ? { ...m, progress: level + 1 } : m
        )
      );
    }
  }, [exp, expToNextLevel, level]);

  // AI 탭 핸들러
  const handleTap = () => {
    const now = Date.now();
    const bonus = now - lastTapTime < 500 ? tapStreak + 1 : 1;
    const coinsEarned = tapPower * bonus;

    setCoins((prev) => prev + coinsEarned);
    setExp((prev) => prev + expMultiplier);
    setTotalTaps((prev) => prev + 1);
    setTapStreak(bonus);
    setLastTapTime(now);
    setMood("happy");

    // 탭 미션 진행도 업데이트
    setMissions((prev) =>
      prev.map((m) =>
        m.id === "tap100" && !m.completed
          ? { ...m, progress: Math.min(m.progress + 1, m.target) }
          : m
      )
    );

    // 코인 미션 진행도 업데이트
    setMissions((prev) =>
      prev.map((m) =>
        m.id === "coins500" && !m.completed
          ? { ...m, progress: Math.min(coins + coinsEarned, m.target) }
          : m
      )
    );
  };

  // 업그레이드 구매
  const handlePurchaseUpgrade = (upgradeId: string) => {
    const upgrade = upgrades.find((u) => u.id === upgradeId);
    if (!upgrade) return;

    const cost = Math.floor(upgrade.baseCost * Math.pow(1.5, upgrade.level));
    if (coins < cost) return;

    setCoins((prev) => prev - cost);
    setUpgrades((prev) =>
      prev.map((u) =>
        u.id === upgradeId
          ? {
              ...u,
              level: u.level + 1,
              effect:
                u.id === "tap"
                  ? u.level + 1
                  : u.id === "auto"
                  ? (u.level + 1) * 2
                  : u.id === "multiplier"
                  ? u.level + 1
                  : u.level + 1,
            }
          : u
      )
    );

    // 업그레이드 미션 진행도 업데이트
    setMissions((prev) =>
      prev.map((m) =>
        m.id === "upgrade3" && !m.completed
          ? { ...m, progress: Math.min(m.progress + 1, m.target) }
          : m
      )
    );

    setMood("excited");
    setTimeout(() => setMood("happy"), 1000);
  };

  // 미션 보상 받기
  const handleClaimMission = (missionId: string) => {
    const mission = missions.find((m) => m.id === missionId);
    if (!mission || mission.completed || mission.progress < mission.target) return;

    setCoins((prev) => prev + mission.reward);
    setMissions((prev) =>
      prev.map((m) => (m.id === missionId ? { ...m, completed: true } : m))
    );
    setMood("excited");
    setTimeout(() => setMood("happy"), 1500);
  };

  const uncompletedMissions = missions.filter((m) => !m.completed).length;

  return (
    <div className="relative min-h-screen w-full max-w-md mx-auto bg-gradient-to-b from-slate-900 via-purple-900 to-pink-900 overflow-hidden">
      {/* 배경 애니메이션 */}
      <div className="absolute inset-0 overflow-hidden">
        {Array.from({ length: 30 }).map((_, i) => (
          <motion.div
            key={i}
            className="absolute w-2 h-2 bg-white rounded-full"
            style={{
              left: `${Math.random() * 100}%`,
              top: `${Math.random() * 100}%`,
            }}
            animate={{
              opacity: [0, 1, 0],
              scale: [0, 1, 0],
            }}
            transition={{
              duration: 3 + Math.random() * 2,
              repeat: Infinity,
              delay: Math.random() * 3,
            }}
          />
        ))}
      </div>

      {/* 메인 컨텐츠 */}
      <div className="relative z-10 flex flex-col h-screen">
        {/* 상단 코인 및 레벨 정보 */}
        <div className="p-4 space-y-3">
          <CoinDisplay coins={coins} coinsPerSecond={autoIncomePerSecond} />

          {/* 레벨 바 */}
          <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-4 border border-white/20">
            <div className="flex items-center justify-between mb-2">
              <span className="text-white font-semibold">레벨 {level}</span>
              <span className="text-gray-300 text-sm">
                {exp} / {expToNextLevel}
              </span>
            </div>
            <div className="h-3 bg-black/30 rounded-full overflow-hidden">
              <motion.div
                className="h-full bg-gradient-to-r from-blue-500 via-purple-500 to-pink-500"
                initial={{ width: 0 }}
                animate={{ width: `${(exp / expToNextLevel) * 100}%` }}
                transition={{ duration: 0.3 }}
              />
            </div>
          </div>
        </div>

        {/* 탭 영역 */}
        <AnimatePresence mode="wait">
          {activeTab === "home" && (
            <motion.div
              key="home"
              className="flex-1 flex flex-col items-center justify-center px-4"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
            >
              <AICharacter level={level} mood={mood} onTap={handleTap} />

              {/* 탭 정보 */}
              <motion.div className="mt-8 space-y-2 text-center">
                <p className="text-white/60 text-sm">AI를 탭하여 코인을 획득하세요!</p>
                {tapStreak > 1 && (
                  <motion.div
                    className="text-yellow-400 font-bold"
                    initial={{ scale: 0 }}
                    animate={{ scale: 1 }}
                    key={tapStreak}
                  >
                    🔥 {tapStreak}x 콤보!
                  </motion.div>
                )}
                <div className="text-gray-400 text-xs">
                  총 탭 횟수: {totalTaps.toLocaleString()}
                </div>
              </motion.div>
            </motion.div>
          )}

          {activeTab === "shop" && (
            <motion.div
              key="shop"
              className="flex-1 overflow-y-auto px-4 pb-4 space-y-3"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
            >
              <h2 className="text-white text-xl font-bold mb-4">상점</h2>
              {upgrades.map((upgrade, index) => (
                <motion.div
                  key={upgrade.id}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: index * 0.1 }}
                >
                  <UpgradeCard
                    icon={upgrade.icon}
                    title={upgrade.title}
                    description={upgrade.description}
                    level={upgrade.level}
                    cost={Math.floor(upgrade.baseCost * Math.pow(1.5, upgrade.level))}
                    coins={coins}
                    onPurchase={() => handlePurchaseUpgrade(upgrade.id)}
                    color={upgrade.color}
                  />
                </motion.div>
              ))}
            </motion.div>
          )}

          {activeTab === "mission" && (
            <motion.div
              key="mission"
              className="flex-1 overflow-y-auto px-4 pb-4 space-y-3"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
            >
              <h2 className="text-white text-xl font-bold mb-4">일일 미션</h2>
              {missions.map((mission, index) => (
                <motion.div
                  key={mission.id}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: index * 0.1 }}
                >
                  <MissionCard
                    title={mission.title}
                    progress={mission.progress}
                    target={mission.target}
                    reward={mission.reward}
                    completed={mission.completed}
                    onClaim={() => handleClaimMission(mission.id)}
                  />
                </motion.div>
              ))}
            </motion.div>
          )}
        </AnimatePresence>

        {/* 하단 탭 네비게이션 */}
        <div className="bg-black/40 backdrop-blur-xl border-t border-white/10 p-2">
          <div className="flex items-center gap-1">
            <TabButton
              icon={Home}
              label="홈"
              active={activeTab === "home"}
              onClick={() => setActiveTab("home")}
            />
            <TabButton
              icon={ShoppingBag}
              label="상점"
              active={activeTab === "shop"}
              onClick={() => setActiveTab("shop")}
            />
            <TabButton
              icon={Trophy}
              label="미션"
              active={activeTab === "mission"}
              onClick={() => setActiveTab("mission")}
              badge={uncompletedMissions}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
