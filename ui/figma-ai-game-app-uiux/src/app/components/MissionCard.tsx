import { motion } from "motion/react";
import { Check } from "lucide-react";

interface MissionCardProps {
  title: string;
  progress: number;
  target: number;
  reward: number;
  completed: boolean;
  onClaim: () => void;
}

export function MissionCard({
  title,
  progress,
  target,
  reward,
  completed,
  onClaim,
}: MissionCardProps) {
  const percentage = Math.min((progress / target) * 100, 100);
  const canClaim = progress >= target && !completed;

  return (
    <motion.div
      className="relative p-4 rounded-xl bg-gradient-to-br from-purple-500/10 to-pink-500/10 border border-purple-500/30"
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <div className="flex items-center justify-between mb-2">
        <h4 className="text-white font-medium text-sm">{title}</h4>
        {completed && (
          <div className="w-6 h-6 rounded-full bg-green-500 flex items-center justify-center">
            <Check className="w-4 h-4 text-white" />
          </div>
        )}
      </div>

      {/* 진행바 */}
      <div className="relative h-2 bg-black/30 rounded-full overflow-hidden mb-2">
        <motion.div
          className="h-full bg-gradient-to-r from-purple-500 to-pink-500"
          initial={{ width: 0 }}
          animate={{ width: `${percentage}%` }}
          transition={{ duration: 0.5 }}
        />
      </div>

      <div className="flex items-center justify-between">
        <span className="text-xs text-gray-400">
          {progress} / {target}
        </span>
        {canClaim ? (
          <motion.button
            onClick={onClaim}
            className="px-4 py-1 bg-gradient-to-r from-purple-500 to-pink-500 rounded-full text-white text-xs font-bold"
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
          >
            보상 받기 💎{reward}
          </motion.button>
        ) : (
          <span className="text-xs text-purple-400">💎 {reward}</span>
        )}
      </div>
    </motion.div>
  );
}
