import { motion } from "motion/react";
import { Brain, Cpu, Zap } from "lucide-react";
import { useEffect, useState } from "react";

interface AICoreProps {
  level: number;
}

export function AICore({ level }: AICoreProps) {
  const [pulseColor, setPulseColor] = useState("#06b6d4");

  useEffect(() => {
    // 레벨에 따라 색상 변경
    if (level < 5) setPulseColor("#06b6d4"); // cyan
    else if (level < 10) setPulseColor("#3b82f6"); // blue
    else if (level < 15) setPulseColor("#8b5cf6"); // violet
    else setPulseColor("#a855f7"); // purple
  }, [level]);

  return (
    <div className="relative flex items-center justify-center">
      {/* 외부 원형 링들 */}
      <motion.div
        className="absolute w-72 h-72 rounded-full border-2 opacity-20"
        style={{ borderColor: pulseColor }}
        animate={{
          scale: [1, 1.2, 1],
          opacity: [0.2, 0.5, 0.2],
        }}
        transition={{
          duration: 3,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      />
      
      <motion.div
        className="absolute w-64 h-64 rounded-full border-2 opacity-30"
        style={{ borderColor: pulseColor }}
        animate={{
          scale: [1, 1.15, 1],
          opacity: [0.3, 0.6, 0.3],
        }}
        transition={{
          duration: 2.5,
          repeat: Infinity,
          ease: "easeInOut",
          delay: 0.5,
        }}
      />

      {/* 메인 코어 */}
      <motion.div
        className="relative w-56 h-56 rounded-full flex items-center justify-center"
        style={{
          background: `radial-gradient(circle, ${pulseColor}40 0%, ${pulseColor}10 50%, transparent 70%)`,
          boxShadow: `0 0 60px ${pulseColor}60, inset 0 0 60px ${pulseColor}40`,
        }}
        animate={{
          boxShadow: [
            `0 0 60px ${pulseColor}60, inset 0 0 60px ${pulseColor}40`,
            `0 0 80px ${pulseColor}80, inset 0 0 80px ${pulseColor}60`,
            `0 0 60px ${pulseColor}60, inset 0 0 60px ${pulseColor}40`,
          ],
        }}
        transition={{
          duration: 2,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      >
        {/* 내부 육각형 패턴 */}
        <div className="absolute inset-0 flex items-center justify-center">
          {[0, 60, 120, 180, 240, 300].map((rotation, i) => (
            <motion.div
              key={i}
              className="absolute w-20 h-1 rounded-full"
              style={{
                background: `linear-gradient(90deg, transparent, ${pulseColor}, transparent)`,
              }}
              animate={{
                rotate: rotation + 360,
                opacity: [0.5, 1, 0.5],
              }}
              transition={{
                rotate: {
                  duration: 20,
                  repeat: Infinity,
                  ease: "linear",
                },
                opacity: {
                  duration: 2,
                  repeat: Infinity,
                  ease: "easeInOut",
                  delay: i * 0.2,
                },
              }}
            />
          ))}
        </div>

        {/* 중앙 아이콘들 */}
        <motion.div
          className="relative z-10 flex items-center justify-center"
          animate={{ rotate: 360 }}
          transition={{ duration: 30, repeat: Infinity, ease: "linear" }}
        >
          <div className="relative">
            <Brain
              className="w-16 h-16 absolute top-0 left-0"
              style={{ color: pulseColor }}
              strokeWidth={1.5}
            />
            <Cpu
              className="w-12 h-12 absolute top-2 left-2"
              style={{ color: pulseColor, opacity: 0.6 }}
              strokeWidth={1.5}
            />
            <Zap
              className="w-8 h-8 absolute top-4 left-4"
              style={{ color: pulseColor, opacity: 0.4 }}
              strokeWidth={1.5}
            />
          </div>
        </motion.div>

        {/* 회전하는 궤도 포인트들 */}
        {[0, 120, 240].map((angle, i) => (
          <motion.div
            key={i}
            className="absolute w-2 h-2 rounded-full"
            style={{
              backgroundColor: pulseColor,
              boxShadow: `0 0 10px ${pulseColor}`,
            }}
            animate={{
              rotate: angle - 360,
            }}
            transition={{
              duration: 5,
              repeat: Infinity,
              ease: "linear",
              delay: i * 0.3,
            }}
          >
            <div className="w-2 h-2" style={{ transform: "translateX(110px)" }} />
          </motion.div>
        ))}
      </motion.div>

      {/* 레벨 표시 */}
      <motion.div
        className="absolute -bottom-4 px-6 py-2 rounded-full"
        style={{
          background: `linear-gradient(135deg, ${pulseColor}30, ${pulseColor}10)`,
          border: `1px solid ${pulseColor}60`,
          boxShadow: `0 0 20px ${pulseColor}40`,
        }}
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.5 }}
      >
        <span
          className="font-mono tracking-wider"
          style={{ color: pulseColor }}
        >
          LEVEL {level}
        </span>
      </motion.div>
    </div>
  );
}
