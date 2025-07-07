using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PongHub.UI.Testing
{
    /// <summary>
    /// 交互测试数据记录结构
    /// </summary>
    [Serializable]
    public struct InteractionRecord
    {
        public float Distance;      // 交互距离
        public bool Success;        // 是否成功
        public float ResponseTime;  // 响应时间
        public float Precision;     // 精确度 (0-1)
        public DateTime Timestamp;  // 时间戳
    }

    /// <summary>
    /// 交互测试数据
    /// 用于存储和分析菜单交互测试数据
    /// </summary>
    public class InteractionTestData
    {
        // 按距离分组的交互记录
        public Dictionary<float, List<InteractionRecord>> InteractionsByDistance { get; private set; }

        // 按距离分组的成功率
        public Dictionary<float, float> SuccessRateByDistance { get; private set; }

        // 按距离分组的平均响应时间
        public Dictionary<float, float> AverageResponseTimeByDistance { get; private set; }

        // 按距离分组的平均精确度
        public Dictionary<float, float> AveragePrecisionByDistance { get; private set; }

        // 用户评分 (1-5)
        public List<int> UserRatings { get; private set; }

        // 测试开始时间
        public DateTime TestStartTime { get; private set; }

        // 测试结束时间
        public DateTime TestEndTime { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public InteractionTestData()
        {
            Reset();
        }

        /// <summary>
        /// 重置所有数据
        /// </summary>
        public void Reset()
        {
            InteractionsByDistance = new Dictionary<float, List<InteractionRecord>>();
            SuccessRateByDistance = new Dictionary<float, float>();
            AverageResponseTimeByDistance = new Dictionary<float, float>();
            AveragePrecisionByDistance = new Dictionary<float, float>();
            UserRatings = new List<int>();
            TestStartTime = DateTime.Now;
        }

        /// <summary>
        /// 添加交互记录
        /// </summary>
        /// <param name="distance">交互距离</param>
        /// <param name="success">是否成功</param>
        /// <param name="responseTime">响应时间</param>
        /// <param name="precision">精确度 (可选，默认为1.0)</param>
        public void AddInteraction(float distance, bool success, float responseTime, float precision = 1.0f)
        {
            // 确保列表存在
            if (!InteractionsByDistance.ContainsKey(distance))
            {
                InteractionsByDistance[distance] = new List<InteractionRecord>();
            }

            // 创建记录
            InteractionRecord record = new InteractionRecord
            {
                Distance = distance,
                Success = success,
                ResponseTime = responseTime,
                Precision = precision,
                Timestamp = DateTime.Now
            };

            // 添加记录
            InteractionsByDistance[distance].Add(record);

            // 更新统计数据
            UpdateStatistics(distance);
        }

        /// <summary>
        /// 完成特定距离的测试
        /// </summary>
        /// <param name="distance">交互距离</param>
        public void CompleteDistanceTest(float distance)
        {
            // 确保统计数据已更新
            UpdateStatistics(distance);

            // 记录日志
            Debug.Log($"完成距离 {distance}m 的测试。成功率: {SuccessRateByDistance[distance]:P2}, 平均响应时间: {AverageResponseTimeByDistance[distance]:F3}秒");
        }

        /// <summary>
        /// 添加用户评分
        /// </summary>
        /// <param name="rating">评分 (1-5)</param>
        public void AddUserRating(int rating)
        {
            if (rating >= 1 && rating <= 5)
            {
                UserRatings.Add(rating);
            }
        }

        /// <summary>
        /// 完成所有测试
        /// </summary>
        public void CompleteAllTests()
        {
            TestEndTime = DateTime.Now;

            // 记录总结
            StringBuilder summary = new StringBuilder();
            summary.AppendLine($"测试总结 ({TestStartTime} - {TestEndTime}):");

            foreach (var distance in InteractionsByDistance.Keys)
            {
                summary.AppendLine($"距离 {distance}m: 成功率 {SuccessRateByDistance[distance]:P2}, 平均响应时间 {AverageResponseTimeByDistance[distance]:F3}秒");
            }

            if (UserRatings.Count > 0)
            {
                float avgRating = 0;
                foreach (int rating in UserRatings)
                {
                    avgRating += rating;
                }
                avgRating /= UserRatings.Count;

                summary.AppendLine($"平均用户评分: {avgRating:F1}/5.0");
            }

            Debug.Log(summary.ToString());
        }

        /// <summary>
        /// 生成测试报告
        /// </summary>
        /// <returns>报告文件路径</returns>
        public string GenerateReport()
        {
            // 完成所有测试
            CompleteAllTests();

            // 创建报告文件名
            string fileName = $"MenuInteractionTest_{TestEndTime:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // 写入标题
                    writer.WriteLine("距离(m),成功率(%),平均响应时间(s),平均精确度,交互次数");

                    // 写入每个距离的数据
                    foreach (float distance in InteractionsByDistance.Keys)
                    {
                        writer.WriteLine($"{distance},{SuccessRateByDistance[distance] * 100:F1},{AverageResponseTimeByDistance[distance]:F3},{AveragePrecisionByDistance[distance]:F3},{InteractionsByDistance[distance].Count}");
                    }

                    // 写入用户评分
                    writer.WriteLine();
                    writer.WriteLine("用户评分");

                    foreach (int rating in UserRatings)
                    {
                        writer.WriteLine(rating);
                    }

                    // 写入详细交互记录
                    writer.WriteLine();
                    writer.WriteLine("详细交互记录");
                    writer.WriteLine("距离(m),成功,响应时间(s),精确度,时间戳");

                    foreach (var distanceGroup in InteractionsByDistance)
                    {
                        foreach (var record in distanceGroup.Value)
                        {
                            writer.WriteLine($"{record.Distance},{record.Success},{record.ResponseTime:F3},{record.Precision:F3},{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                        }
                    }
                }

                Debug.Log($"测试报告已保存到: {filePath}");
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"生成报告失败: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取最佳交互距离
        /// </summary>
        /// <returns>最佳交互距离</returns>
        public float GetBestInteractionDistance()
        {
            float bestDistance = 0;
            float bestScore = -1;

            foreach (float distance in InteractionsByDistance.Keys)
            {
                // 计算综合得分 (成功率 * 0.6 + 精确度 * 0.3 + 响应时间得分 * 0.1)
                float successScore = SuccessRateByDistance[distance];
                float precisionScore = AveragePrecisionByDistance[distance];
                float responseTimeScore = Mathf.Clamp01(1.0f - AverageResponseTimeByDistance[distance] / 2.0f); // 响应时间越短越好，最大2秒

                float score = successScore * 0.6f + precisionScore * 0.3f + responseTimeScore * 0.1f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDistance = distance;
                }
            }

            return bestDistance;
        }

        /// <summary>
        /// 获取特定距离的成功率
        /// </summary>
        /// <param name="distance">交互距离</param>
        /// <returns>成功率 (0-1)</returns>
        public float GetSuccessRate(float distance)
        {
            if (SuccessRateByDistance.ContainsKey(distance))
            {
                return SuccessRateByDistance[distance];
            }

            return 0;
        }

        /// <summary>
        /// 获取特定距离的平均响应时间
        /// </summary>
        /// <param name="distance">交互距离</param>
        /// <returns>平均响应时间 (秒)</returns>
        public float GetAverageResponseTime(float distance)
        {
            if (AverageResponseTimeByDistance.ContainsKey(distance))
            {
                return AverageResponseTimeByDistance[distance];
            }

            return 0;
        }

        /// <summary>
        /// 获取特定距离的平均精确度
        /// </summary>
        /// <param name="distance">交互距离</param>
        /// <returns>平均精确度 (0-1)</returns>
        public float GetAveragePrecision(float distance)
        {
            if (AveragePrecisionByDistance.ContainsKey(distance))
            {
                return AveragePrecisionByDistance[distance];
            }

            return 0;
        }

        /// <summary>
        /// 获取平均用户评分
        /// </summary>
        /// <returns>平均评分 (1-5)</returns>
        public float GetAverageUserRating()
        {
            if (UserRatings.Count == 0)
            {
                return 0;
            }

            float sum = 0;
            foreach (int rating in UserRatings)
            {
                sum += rating;
            }

            return sum / UserRatings.Count;
        }

        /// <summary>
        /// 更新统计数据
        /// </summary>
        /// <param name="distance">交互距离</param>
        private void UpdateStatistics(float distance)
        {
            if (!InteractionsByDistance.ContainsKey(distance) || InteractionsByDistance[distance].Count == 0)
            {
                return;
            }

            // 计算成功率
            int successCount = 0;
            float responseTimeSum = 0;
            float precisionSum = 0;

            foreach (var record in InteractionsByDistance[distance])
            {
                if (record.Success)
                {
                    successCount++;
                }

                responseTimeSum += record.ResponseTime;
                precisionSum += record.Precision;
            }

            int totalCount = InteractionsByDistance[distance].Count;
            SuccessRateByDistance[distance] = (float)successCount / totalCount;
            AverageResponseTimeByDistance[distance] = responseTimeSum / totalCount;
            AveragePrecisionByDistance[distance] = precisionSum / totalCount;
        }
    }
}