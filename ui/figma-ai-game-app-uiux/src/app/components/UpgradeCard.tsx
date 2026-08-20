import { motion } from "motion/react";
import { LucideIcon } from "lucide-react";

interface UpgradeCardProps {
  icon: LucideIcon;
  title: string;
  description: string;
  level: number;
  cost: number;
  coins: number;
  onPurchase: () => void;
  color: string;
}

export function UpgradeCard({
  icon: Icon,
  title,
  description,
  level,
  cost,
  coins,
  onPurchase,
  color,
}: UpgradeCardProps) {
  const canAfford = coins >= cost;

  return (
    <motion.button
      onClick={canAfford ? onPurchase : undefined}
      disabled={!canAfford}
      className="relative w-full p-4 rounded-2xl bg-white/5 backdrop-blur-sm border-2 text-left overflow-hidden"
      style={{
        borderColor: canAfford ? color : "#ffffff20",
      }}
      whileHover={canAfford ? { scale: 1.02, y: -2 } : {}}
      whileTap={canAfford ? { scale: 0.98 } : {}}
      initial={{ opacity: 0, x: -20 }}
      animate={{ opacity: 1, x: 0 }}
    >
      {/* 배경 그라데이션 */}
      <div
        className="absolute inset-0 opacity-10"
        style={{
          background: `linear-gradient(135deg, ${color}, transparent)`,
        }}
      />

      {/* 내용 */}
      <div className="relative flex items-start gap-3">
        {/* 아이콘 */}
        <div
          className="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0"
          style={{
            backgroundColor: `${color}20`,
            border: `2px solid ${color}40`,
          }}
        >
          <Icon className="w-6 h-6" style={{ color }} />
        </div>

        {/* 정보 */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2 mb-1">
            <h3 className="font-semibold text-white">{title}</h3>
            <span
              className="text-xs font-bold px-2 py-1 rounded-full"
              style={{
                backgroundColor: `${color}30`,
                color: color,
              }}
            >
              Lv.{level}
            </span>
          </div>
          <p className="text-xs text-gray-400 mb-2">{description}</p>
          
          {/* 비용 */}
          <div className="flex items-center gap-2">
            <div
              className="flex items-center gap-1 px-3 py-1.5 rounded-full"
              style={{
                backgroundColor: canAfford ? `${color}20` : "#ffffff10",
                border: `1px solid ${canAfford ? color : "#ffffff20"}`,
              }}
            >
              <span className="text-lg">💎</span>
              <span
                className="font-bold text-sm"
                style={{ color: canAfford ? color : "#ffffff60" }}
              >
                {cost.toLocaleString()}
              </span>
            </div>

            {!canAfford && (
              <span className="text-xs text-red-400">코인 부족</span>
            )}
          </div>
        </div>
      </div>

      {/* 반짝이는 효과 (구매 가능할 때) */}
      {canAfford && (
        <motion.div
          className="absolute inset-0 pointer-events-none"
          style={{
            background: `linear-gradient(90deg, transparent, ${color}30, transparent)`,
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
      )}
    </motion.button>
  );
}
