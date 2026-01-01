using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SeiWoLauncherPro.Utils {
    /// <summary>
    ///     高精度速度追踪器
    ///     <para>使用线性回归算法计算手势的瞬时速度，具备抗抖动和抗系统卡顿能力。</para>
    /// </summary>
    public class VelocityTracker {
        // 采样窗口时间 (ms)。只保留最近 150ms 的数据用于计算。
        // 太长会导致“由于惯性”反应迟钝，太短容易受手抖影响。
        private const long HorizonMilliseconds = 150;

        // 最小采样点数。少于这个点数认为数据不足，无法计算速度。
        private const int MinSamplePoints = 2;

        // 缓存频率倒数，避免除法重复计算
        private readonly double _frequencyInverse;

        // 这里的 List 用作环形缓冲区的逻辑实现
        // 由于手势采样点通常不多（一秒60-144个），List 的移除开销可以忽略不计
        private readonly List<DataPoint> _samples;

        public VelocityTracker() {
            _samples = new List<DataPoint>(32); // 预分配容量，减少 GC
            _frequencyInverse = 1.0 / Stopwatch.Frequency;
        }

        /// <summary>
        ///     在 MouseDown 时调用，重置追踪器
        /// </summary>
        public void Clear() {
            _samples.Clear();
        }

        /// <summary>
        ///     在 MouseMove 时调用，添加采样点
        /// </summary>
        /// <param name="position">当前的逻辑坐标 (Pixels)</param>
        public void AddPosition(double position) {
            long now = Stopwatch.GetTimestamp();

            // 1. 添加新点
            _samples.Add(new DataPoint(now, position));

            // 2. 剪枝：剔除超时的数据 (Pruning)
            // 我们只需要最近 HorizonMilliseconds 内的数据
            PruneHistory(now);
        }

        /// <summary>
        ///     在 MouseUp 时调用，计算最终速度
        /// </summary>
        /// <returns>速度 (Pixels / Second)</returns>
        public double ComputeVelocity() {
            int count = _samples.Count;
            if (count < MinSamplePoints) {
                return 0;
            }

            // 获取最新点用于修剪（以防计算时已经过了很久，虽然这种情况很少）
            DataPoint newest = _samples[count - 1];

            // === 核心算法：线性回归 (Linear Regression) ===
            // 我们试图拟合直线 P = v * t + c，其中斜率 v 就是速度。
            // 使用公式：v = S_xy / S_xx (简化版，假设通过重心)

            // 为了数值稳定性，我们将时间归一化，以最早的那个点为 t=0
            long startTime = _samples[0].Timestamp;

            double sumX = 0; // sum(time)
            double sumY = 0; // sum(position)
            double sumXY = 0; // sum(time * position)
            double sumXX = 0; // sum(time * time)

            for (int i = 0; i < count; i++) {
                DataPoint p = _samples[i];

                // 将 Tick 转换为秒 (Seconds)，作为 X 轴
                double t = (p.Timestamp - startTime) * _frequencyInverse;
                double pos = p.Position;

                sumX += t;
                sumY += pos;
                sumXY += t * pos;
                sumXX += t * t;
            }

            // 计算均值
            double meanX = sumX / count;
            double meanY = sumY / count;

            // 计算斜率 (Slope)
            // formula: (sum(xy) - n * mean_x * mean_y) / (sum(xx) - n * mean_x^2)
            double numerator = sumXY - (count * meanX * meanY);
            double denominator = sumXX - (count * meanX * meanX);

            // 防止除以零 (例如所有点都在同一时间戳，虽然不可能)
            if (Math.Abs(denominator) < 1e-9) {
                return 0;
            }

            double velocity = numerator / denominator;

            return velocity;
        }

        /// <summary>
        ///     剔除超出时间窗口的老化数据
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PruneHistory(long now) {
            // 将 ms 转换为 tick 差值
            long horizonTicks = (long)(HorizonMilliseconds / 1000.0 * Stopwatch.Frequency);
            long deadline = now - horizonTicks;

            // 从头开始移除，直到剩下的点都在时间窗口内
            // 考虑到 List 内部是数组拷贝，这里只要移除前面的几个点，性能损耗微乎其微
            while (_samples.Count > 0 && _samples[0].Timestamp < deadline) {
                _samples.RemoveAt(0);
            }
        }

        // 内部数据结构：记录时间戳和位置
        private readonly struct DataPoint {
            public readonly long Timestamp; // 原始 Tick
            public readonly double Position; // 逻辑像素

            public DataPoint(long timestamp, double position) {
                Timestamp = timestamp;
                Position = position;
            }
        }
    }
}