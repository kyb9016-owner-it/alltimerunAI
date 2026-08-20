import { motion } from "motion/react";

interface StatBarProps {
  label: string;
  value: number;
  maxValue: number;
  color: string;
  icon: React.ReactNode;
}

export function StatBar({ label, value, maxValue, color, icon }: StatBarProps) {
  const percentage = (value / maxValue) * 100;

  return (
    <div className="relative">
      {/* 레이블 */}
      <div className="flex items-center gap-2 mb-2">
        <div style={{ color }}>{icon}</div>
        <span className="text-cyan-300 text-sm font-mono tracking-wide uppercase">
          {label}
        </span>
        <span className="ml-auto text-cyan-400 font-mono text-sm">
          {value}/{maxValue}
        </span>
      </div>

      {/* 프로그레스 바 배경 */}
      <div
        className="relative h-2 rounded-full overflow-hidden"
        style={{
          backgroundColor: "#0a1628",
          border: `1px solid ${color}30`,
          boxShadow: `inset 0 0 10px ${color}20`,
        }}
      >
        {/* 프로그레스 바 채우기 */}
        <motion.div
          className="h-full rounded-full"
          style={{
            background: `linear-gradient(90deg, ${color}80, ${color})`,
            boxShadow: `0 0 10px ${color}, inset 0 1px 0 ${color}40`,
          }}
          initial={{ width: 0 }}
          animate={{ width: `${percentage}%` }}
          transition={{ duration: 1, ease: "easeOut" }}
        />

        {/* 반짝이는 효과 */}
        <motion.div
          className="absolute top-0 left-0 h-full w-full"
          style={{
            background: `linear-gradient(90deg, transparent, ${color}60, transparent)`,
          }}
          animate={{
            x: ["-100%", "200%"],
          }}
          transition={{
            duration: 2,
            repeat: Infinity,
            ease: "linear",
          }}
        />
      </div>

      {/* 격자 패턴 */}
      <div className="absolute top-8 left-0 right-0 h-2 flex gap-1">
        {Array.from({ length: 20 }).map((_, i) => (
          <div
            key={i}
            className="flex-1 h-full"
            style={{
              borderRight: `1px solid ${color}10`,
            }}
          />
        ))}
      </div>
    </div>
  );
}
