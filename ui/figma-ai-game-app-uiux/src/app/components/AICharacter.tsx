import { motion } from "motion/react";
import { Sparkles, Zap, Heart } from "lucide-react";
import { useEffect, useState } from "react";

interface AICharacterProps {
  level: number;
  mood: "happy" | "excited" | "thinking";
  onTap: () => void;
}

export function AICharacter({ level, mood, onTap }: AICharacterProps) {
  const [particles, setParticles] = useState<{ id: number; x: number; y: number }[]>([]);

  // 레벨에 따른 진화 단계
  const getEvolutionStage = () => {
    if (level >= 50) return 5;
    if (level >= 30) return 4;
    if (level >= 20) return 3;
    if (level >= 10) return 2;
    return 1;
  };

  const stage = getEvolutionStage();
  
  const getColor = () => {
    const colors = ["#60a5fa", "#a78bfa", "#ec4899", "#f59e0b", "#10b981"];
    return colors[stage - 1];
  };

  const handleTap = (e: React.MouseEvent) => {
    onTap();
    
    // 탭 위치에 파티클 생성
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    const newParticle = { id: Date.now(), x, y };
    setParticles((prev) => [...prev, newParticle]);
    
    setTimeout(() => {
      setParticles((prev) => prev.filter((p) => p.id !== newParticle.id));
    }, 1000);
  };

  return (
    <div className="relative flex items-center justify-center">
      <motion.div
        onClick={handleTap}
        className="relative cursor-pointer select-none"
        whileTap={{ scale: 0.9 }}
        animate={{ 
          y: [-5, 5, -5],
        }}
        transition={{
          y: {
            duration: 2,
            repeat: Infinity,
            ease: "easeInOut",
          }
        }}
      >
        {/* 후광 효과 */}
        <motion.div
          className="absolute inset-0 -m-8 rounded-full blur-3xl opacity-50"
          style={{ backgroundColor: getColor() }}
          animate={{
            scale: [1, 1.2, 1],
            opacity: [0.3, 0.5, 0.3],
          }}
          transition={{
            duration: 2,
            repeat: Infinity,
            ease: "easeInOut",
          }}
        />

        {/* 메인 바디 */}
        <div className="relative">
          {/* 몸통 */}
          <motion.div
            className="w-40 h-40 rounded-full flex items-center justify-center relative"
            style={{
              background: `linear-gradient(135deg, ${getColor()}, ${getColor()}dd)`,
              boxShadow: `0 10px 40px ${getColor()}80, inset 0 -5px 20px rgba(0,0,0,0.2)`,
            }}
            animate={{
              rotate: mood === "thinking" ? [0, 5, -5, 0] : 0,
            }}
            transition={{
              duration: 0.5,
              repeat: mood === "thinking" ? Infinity : 0,
            }}
          >
            {/* 반짝이는 하이라이트 */}
            <motion.div
              className="absolute top-8 left-12 w-12 h-12 rounded-full bg-white"
              style={{ opacity: 0.3 }}
              animate={{
                scale: [1, 1.1, 1],
                opacity: [0.3, 0.4, 0.3],
              }}
              transition={{
                duration: 1.5,
                repeat: Infinity,
              }}
            />

            {/* 눈 */}
            <div className="absolute inset-0 flex items-center justify-center gap-8 mt-2">
              <motion.div
                className="w-4 h-6 bg-white rounded-full"
                animate={mood === "happy" ? {
                  scaleY: [1, 0.1, 1],
                } : {}}
                transition={{
                  duration: 3,
                  repeat: Infinity,
                  repeatDelay: 2,
                }}
              />
              <motion.div
                className="w-4 h-6 bg-white rounded-full"
                animate={mood === "happy" ? {
                  scaleY: [1, 0.1, 1],
                } : {}}
                transition={{
                  duration: 3,
                  repeat: Infinity,
                  repeatDelay: 2,
                }}
              />
            </div>

            {/* 입 */}
            <motion.div
              className="absolute bottom-12 left-1/2 -translate-x-1/2"
              animate={{
                scale: mood === "excited" ? [1, 1.2, 1] : 1,
              }}
              transition={{
                duration: 0.5,
                repeat: mood === "excited" ? Infinity : 0,
              }}
            >
              <div 
                className="w-12 h-6 border-b-4 border-white rounded-b-full"
                style={{ borderBottomWidth: 3 }}
              />
            </motion.div>

            {/* 볼 터치 */}
            <div className="absolute bottom-16 left-8 w-6 h-3 rounded-full bg-white opacity-20" />
            <div className="absolute bottom-16 right-8 w-6 h-3 rounded-full bg-white opacity-20" />
          </motion.div>

          {/* 안테나 */}
          <motion.div
            className="absolute -top-8 left-1/2 -translate-x-1/2 w-1 h-8 bg-gradient-to-t from-current to-transparent"
            style={{ color: getColor() }}
            animate={{
              height: [32, 36, 32],
            }}
            transition={{
              duration: 1.5,
              repeat: Infinity,
            }}
          >
            <motion.div
              className="absolute -top-1 left-1/2 -translate-x-1/2 w-3 h-3 rounded-full"
              style={{ backgroundColor: getColor() }}
              animate={{
                boxShadow: [
                  `0 0 10px ${getColor()}`,
                  `0 0 20px ${getColor()}`,
                  `0 0 10px ${getColor()}`,
                ],
              }}
              transition={{
                duration: 1,
                repeat: Infinity,
              }}
            />
          </motion.div>

          {/* 진화 단계 표시 별 */}
          <div className="absolute -top-4 -right-4 flex gap-1">
            {Array.from({ length: stage }).map((_, i) => (
              <motion.div
                key={i}
                initial={{ scale: 0, rotate: -180 }}
                animate={{ scale: 1, rotate: 0 }}
                transition={{ delay: i * 0.1 }}
              >
                <Sparkles 
                  className="w-4 h-4 fill-yellow-400 text-yellow-400"
                />
              </motion.div>
            ))}
          </div>
        </div>
      </motion.div>

      {/* 탭 파티클 효과 */}
      {particles.map((particle) => (
        <motion.div
          key={particle.id}
          className="absolute pointer-events-none"
          style={{ left: particle.x, top: particle.y }}
          initial={{ opacity: 1, scale: 1, y: 0 }}
          animate={{ opacity: 0, scale: 2, y: -50 }}
          transition={{ duration: 1 }}
        >
          <div className="text-2xl font-bold" style={{ color: getColor() }}>
            +1
          </div>
        </motion.div>
      ))}

      {/* 떠다니는 하트/전기 아이콘 */}
      {mood === "happy" && (
        <motion.div
          className="absolute"
          initial={{ opacity: 0, y: 0, x: -20 }}
          animate={{ 
            opacity: [0, 1, 0],
            y: -60,
            x: [-20, -30, -25],
          }}
          transition={{
            duration: 2,
            repeat: Infinity,
            repeatDelay: 1,
          }}
        >
          <Heart className="w-6 h-6 fill-pink-400 text-pink-400" />
        </motion.div>
      )}
    </div>
  );
}
