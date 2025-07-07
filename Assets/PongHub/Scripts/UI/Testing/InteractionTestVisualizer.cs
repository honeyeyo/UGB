using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PongHub.UI.Testing
{
    /// <summary>
    /// 交互测试数据可视化器
    /// 用于在Unity编辑器中可视化交互测试数据
    /// </summary>
    public class InteractionTestVisualizer : MonoBehaviour
    {
        [Header("图表设置")]
        [SerializeField] private RectTransform m_chartContainer;
        [SerializeField] private GameObject m_barPrefab;
        [SerializeField] private GameObject m_linePrefab;
        [SerializeField] private GameObject m_pointPrefab;
        [SerializeField] private float m_barWidth = 50f;
        [SerializeField] private float m_barSpacing = 20f;
        [SerializeField] private float m_chartHeight = 300f;
        [SerializeField] private float m_chartPadding = 50f;

        [Header("图表颜色")]
        [SerializeField] private Color m_successRateColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color m_responseTimeColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color m_precisionColor = new Color(0.2f, 0.2f, 0.8f);
        [SerializeField] private Color m_gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("图表标签")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private TextMeshProUGUI m_xAxisLabel;
        [SerializeField] private TextMeshProUGUI m_yAxisLabel;
        [SerializeField] private GameObject m_labelPrefab;

        [Header("图表类型")]
        [SerializeField] private bool m_showSuccessRate = true;
        [SerializeField] private bool m_showResponseTime = true;
        [SerializeField] private bool m_showPrecision = true;
        [SerializeField] private bool m_useBarChart = true;
        [SerializeField] private bool m_useLineChart = false;

        // 私有变量
        private InteractionTestData m_testData;
        private List<GameObject> m_chartObjects = new List<GameObject>();
        private List<float> m_distances = new List<float>();

        #region Unity生命周期

        private void Start()
        {
            // 初始化图表
            InitializeChart();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 可视化测试数据
        /// </summary>
        /// <param name="testData">测试数据</param>
        public void VisualizeData(InteractionTestData testData)
        {
            if (testData == null)
                return;

            m_testData = testData;

            // 清除现有图表
            ClearChart();

            // 获取所有测试距离
            m_distances.Clear();
            foreach (var distance in m_testData.InteractionsByDistance.Keys)
            {
                m_distances.Add(distance);
            }
            m_distances.Sort();

            // 绘制图表
            if (m_useBarChart)
            {
                DrawBarChart();
            }

            if (m_useLineChart)
            {
                DrawLineChart();
            }

            // 更新标题
            if (m_titleText != null)
            {
                m_titleText.text = "菜单交互测试结果";
            }

            // 更新坐标轴标签
            if (m_xAxisLabel != null)
            {
                m_xAxisLabel.text = "距离 (米)";
            }

            if (m_yAxisLabel != null)
            {
                m_yAxisLabel.text = "数值";
            }
        }

        /// <summary>
        /// 切换图表类型
        /// </summary>
        /// <param name="useBarChart">是否使用柱状图</param>
        public void ToggleChartType(bool useBarChart)
        {
            m_useBarChart = useBarChart;
            m_useLineChart = !useBarChart;

            if (m_testData != null)
            {
                VisualizeData(m_testData);
            }
        }

        /// <summary>
        /// 切换显示成功率
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ToggleSuccessRate(bool show)
        {
            m_showSuccessRate = show;

            if (m_testData != null)
            {
                VisualizeData(m_testData);
            }
        }

        /// <summary>
        /// 切换显示响应时间
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ToggleResponseTime(bool show)
        {
            m_showResponseTime = show;

            if (m_testData != null)
            {
                VisualizeData(m_testData);
            }
        }

        /// <summary>
        /// 切换显示精确度
        /// </summary>
        /// <param name="show">是否显示</param>
        public void TogglePrecision(bool show)
        {
            m_showPrecision = show;

            if (m_testData != null)
            {
                VisualizeData(m_testData);
            }
        }

        /// <summary>
        /// 导出图表为图片
        /// </summary>
        public void ExportChart()
        {
            if (m_chartContainer == null)
                return;

            // 创建一个RenderTexture
            RenderTexture rt = new RenderTexture(1024, 768, 24);

            // 创建一个Camera来渲染图表
            GameObject cameraObj = new GameObject("ChartCamera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.targetTexture = rt;
            camera.orthographic = true;
            camera.orthographicSize = m_chartContainer.rect.height / 2;
            camera.transform.position = new Vector3(m_chartContainer.position.x, m_chartContainer.position.y, -10);

            // 渲染图表
            camera.Render();

            // 创建一个Texture2D来保存图片
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1024, 768, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0);
            tex.Apply();

            // 保存为PNG
            byte[] bytes = tex.EncodeToPNG();
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"MenuInteractionChart_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            System.IO.File.WriteAllBytes(filePath, bytes);

            // 清理
            RenderTexture.active = null;
            Destroy(cameraObj);
            Destroy(rt);

            Debug.Log($"图表已导出到: {filePath}");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitializeChart()
        {
            if (m_chartContainer == null)
            {
                Debug.LogError("图表容器未设置");
                return;
            }

            // 绘制网格线
            DrawGridLines();
        }

        /// <summary>
        /// 清除图表
        /// </summary>
        private void ClearChart()
        {
            foreach (var obj in m_chartObjects)
            {
                Destroy(obj);
            }

            m_chartObjects.Clear();
        }

        /// <summary>
        /// 绘制网格线
        /// </summary>
        private void DrawGridLines()
        {
            if (m_chartContainer == null)
                return;

            // 水平网格线
            for (int i = 0; i <= 5; i++)
            {
                float y = i * m_chartHeight / 5;

                GameObject line = new GameObject($"HGridLine_{i}");
                line.transform.SetParent(m_chartContainer, false);

                Image lineImage = line.AddComponent<Image>();
                lineImage.color = m_gridLineColor;

                RectTransform rectTransform = line.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.sizeDelta = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(0, y + m_chartPadding);

                m_chartObjects.Add(line);

                // 添加标签
                GameObject label = Instantiate(m_labelPrefab, m_chartContainer);
                TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
                if (labelText != null)
                {
                    labelText.text = $"{i * 0.2:F1}";
                    label.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30, y + m_chartPadding);
                }

                m_chartObjects.Add(label);
            }

            // 垂直网格线将在绘制柱状图时添加
        }

        /// <summary>
        /// 绘制柱状图
        /// </summary>
        private void DrawBarChart()
        {
            if (m_chartContainer == null || m_barPrefab == null || m_distances.Count == 0)
                return;

            // 计算总宽度
            float totalWidth = m_distances.Count * (m_barWidth * 3 + m_barSpacing);
            float startX = -totalWidth / 2 + m_barWidth / 2;

            // 为每个距离绘制柱状图
            for (int i = 0; i < m_distances.Count; i++)
            {
                float distance = m_distances[i];
                float x = startX + i * (m_barWidth * 3 + m_barSpacing);

                // 绘制垂直网格线
                GameObject line = new GameObject($"VGridLine_{i}");
                line.transform.SetParent(m_chartContainer, false);

                Image lineImage = line.AddComponent<Image>();
                lineImage.color = m_gridLineColor;

                RectTransform lineRect = line.GetComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0);
                lineRect.anchorMax = new Vector2(0.5f, 1);
                lineRect.sizeDelta = new Vector2(1, 0);
                lineRect.anchoredPosition = new Vector2(x - m_barWidth - m_barSpacing / 2, m_chartHeight / 2 + m_chartPadding);

                m_chartObjects.Add(line);

                // 添加距离标签
                GameObject distanceLabel = Instantiate(m_labelPrefab, m_chartContainer);
                TextMeshProUGUI distanceLabelText = distanceLabel.GetComponent<TextMeshProUGUI>();
                if (distanceLabelText != null)
                {
                    distanceLabelText.text = $"{distance}m";
                    distanceLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 10);
                }

                m_chartObjects.Add(distanceLabel);

                // 绘制成功率柱状图
                if (m_showSuccessRate && m_testData.SuccessRateByDistance.ContainsKey(distance))
                {
                    float successRate = m_testData.SuccessRateByDistance[distance];
                    DrawBar(x - m_barWidth, successRate, m_successRateColor, "成功率");
                }

                // 绘制响应时间柱状图 (归一化到0-1范围)
                if (m_showResponseTime && m_testData.AverageResponseTimeByDistance.ContainsKey(distance))
                {
                    float responseTime = m_testData.AverageResponseTimeByDistance[distance];
                    float normalizedResponseTime = Mathf.Clamp01(responseTime / 2.0f); // 假设最大响应时间为2秒
                    DrawBar(x, normalizedResponseTime, m_responseTimeColor, $"{responseTime:F3}s");
                }

                // 绘制精确度柱状图
                if (m_showPrecision && m_testData.AveragePrecisionByDistance.ContainsKey(distance))
                {
                    float precision = m_testData.AveragePrecisionByDistance[distance];
                    DrawBar(x + m_barWidth, precision, m_precisionColor, $"{precision:F2}");
                }
            }

            // 添加图例
            DrawLegend();
        }

        /// <summary>
        /// 绘制折线图
        /// </summary>
        private void DrawLineChart()
        {
            if (m_chartContainer == null || m_linePrefab == null || m_pointPrefab == null || m_distances.Count <= 1)
                return;

            // 计算总宽度
            float totalWidth = m_chartContainer.rect.width - m_chartPadding * 2;
            float chartWidth = Mathf.Min(totalWidth, m_distances.Count * 100);
            float startX = -chartWidth / 2;
            float stepX = chartWidth / (m_distances.Count - 1);

            // 绘制成功率折线
            if (m_showSuccessRate)
            {
                List<Vector2> successRatePoints = new List<Vector2>();

                for (int i = 0; i < m_distances.Count; i++)
                {
                    float distance = m_distances[i];
                    float x = startX + i * stepX;

                    if (m_testData.SuccessRateByDistance.ContainsKey(distance))
                    {
                        float successRate = m_testData.SuccessRateByDistance[distance];
                        float y = successRate * m_chartHeight + m_chartPadding;
                        successRatePoints.Add(new Vector2(x, y));

                        // 绘制点
                        DrawPoint(x, y, m_successRateColor);
                    }
                }

                // 绘制线
                DrawLines(successRatePoints, m_successRateColor);
            }

            // 绘制响应时间折线
            if (m_showResponseTime)
            {
                List<Vector2> responseTimePoints = new List<Vector2>();

                for (int i = 0; i < m_distances.Count; i++)
                {
                    float distance = m_distances[i];
                    float x = startX + i * stepX;

                    if (m_testData.AverageResponseTimeByDistance.ContainsKey(distance))
                    {
                        float responseTime = m_testData.AverageResponseTimeByDistance[distance];
                        float normalizedResponseTime = Mathf.Clamp01(responseTime / 2.0f); // 假设最大响应时间为2秒
                        float y = normalizedResponseTime * m_chartHeight + m_chartPadding;
                        responseTimePoints.Add(new Vector2(x, y));

                        // 绘制点
                        DrawPoint(x, y, m_responseTimeColor);
                    }
                }

                // 绘制线
                DrawLines(responseTimePoints, m_responseTimeColor);
            }

            // 绘制精确度折线
            if (m_showPrecision)
            {
                List<Vector2> precisionPoints = new List<Vector2>();

                for (int i = 0; i < m_distances.Count; i++)
                {
                    float distance = m_distances[i];
                    float x = startX + i * stepX;

                    if (m_testData.AveragePrecisionByDistance.ContainsKey(distance))
                    {
                        float precision = m_testData.AveragePrecisionByDistance[distance];
                        float y = precision * m_chartHeight + m_chartPadding;
                        precisionPoints.Add(new Vector2(x, y));

                        // 绘制点
                        DrawPoint(x, y, m_precisionColor);
                    }
                }

                // 绘制线
                DrawLines(precisionPoints, m_precisionColor);
            }

            // 添加图例
            DrawLegend();
        }

        /// <summary>
        /// 绘制柱状图
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="value">值 (0-1)</param>
        /// <param name="color">颜色</param>
        /// <param name="label">标签</param>
        private void DrawBar(float x, float value, Color color, string label)
        {
            if (m_barPrefab == null)
                return;

            // 创建柱状图
            GameObject bar = Instantiate(m_barPrefab, m_chartContainer);
            Image barImage = bar.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.color = color;
            }

            // 设置位置和大小
            RectTransform rectTransform = bar.GetComponent<RectTransform>();
            float height = value * m_chartHeight;
            rectTransform.sizeDelta = new Vector2(m_barWidth, height);
            rectTransform.anchoredPosition = new Vector2(x, height / 2 + m_chartPadding);

            m_chartObjects.Add(bar);

            // 添加标签
            GameObject valueLabel = Instantiate(m_labelPrefab, m_chartContainer);
            TextMeshProUGUI valueLabelText = valueLabel.GetComponent<TextMeshProUGUI>();
            if (valueLabelText != null)
            {
                valueLabelText.text = label;
                valueLabelText.color = color;
                valueLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, height + m_chartPadding + 15);
            }

            m_chartObjects.Add(valueLabel);
        }

        /// <summary>
        /// 绘制点
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="color">颜色</param>
        private void DrawPoint(float x, float y, Color color)
        {
            if (m_pointPrefab == null)
                return;

            // 创建点
            GameObject point = Instantiate(m_pointPrefab, m_chartContainer);
            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = color;
            }

            // 设置位置
            RectTransform rectTransform = point.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(x, y);

            m_chartObjects.Add(point);
        }

        /// <summary>
        /// 绘制线
        /// </summary>
        /// <param name="points">点列表</param>
        /// <param name="color">颜色</param>
        private void DrawLines(List<Vector2> points, Color color)
        {
            if (m_linePrefab == null || points.Count < 2)
                return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[i + 1];

                // 创建线
                GameObject line = Instantiate(m_linePrefab, m_chartContainer);
                Image lineImage = line.GetComponent<Image>();
                if (lineImage != null)
                {
                    lineImage.color = color;
                }

                // 计算线的长度和角度
                float length = Vector2.Distance(start, end);
                float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

                // 设置位置、大小和旋转
                RectTransform rectTransform = line.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(length, 2);
                rectTransform.anchoredPosition = (start + end) / 2;
                rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

                m_chartObjects.Add(line);
            }
        }

        /// <summary>
        /// 绘制图例
        /// </summary>
        private void DrawLegend()
        {
            if (m_labelPrefab == null)
                return;

            float y = -30;
            float x = 0;

            if (m_showSuccessRate)
            {
                // 创建图例项
                GameObject legendItem = new GameObject("SuccessRateLegend");
                legendItem.transform.SetParent(m_chartContainer, false);

                // 创建图例颜色块
                GameObject colorBlock = new GameObject("ColorBlock");
                colorBlock.transform.SetParent(legendItem.transform, false);
                Image colorImage = colorBlock.AddComponent<Image>();
                colorImage.color = m_successRateColor;
                RectTransform colorRect = colorBlock.GetComponent<RectTransform>();
                colorRect.sizeDelta = new Vector2(20, 20);
                colorRect.anchoredPosition = new Vector2(x - 100, y);

                // 创建图例文本
                GameObject textObj = Instantiate(m_labelPrefab, legendItem.transform);
                TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "成功率";
                    text.color = Color.black;
                    textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x - 50, y);
                }

                m_chartObjects.Add(legendItem);
                x += 100;
            }

            if (m_showResponseTime)
            {
                // 创建图例项
                GameObject legendItem = new GameObject("ResponseTimeLegend");
                legendItem.transform.SetParent(m_chartContainer, false);

                // 创建图例颜色块
                GameObject colorBlock = new GameObject("ColorBlock");
                colorBlock.transform.SetParent(legendItem.transform, false);
                Image colorImage = colorBlock.AddComponent<Image>();
                colorImage.color = m_responseTimeColor;
                RectTransform colorRect = colorBlock.GetComponent<RectTransform>();
                colorRect.sizeDelta = new Vector2(20, 20);
                colorRect.anchoredPosition = new Vector2(x - 100, y);

                // 创建图例文本
                GameObject textObj = Instantiate(m_labelPrefab, legendItem.transform);
                TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "响应时间";
                    text.color = Color.black;
                    textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x - 50, y);
                }

                m_chartObjects.Add(legendItem);
                x += 100;
            }

            if (m_showPrecision)
            {
                // 创建图例项
                GameObject legendItem = new GameObject("PrecisionLegend");
                legendItem.transform.SetParent(m_chartContainer, false);

                // 创建图例颜色块
                GameObject colorBlock = new GameObject("ColorBlock");
                colorBlock.transform.SetParent(legendItem.transform, false);
                Image colorImage = colorBlock.AddComponent<Image>();
                colorImage.color = m_precisionColor;
                RectTransform colorRect = colorBlock.GetComponent<RectTransform>();
                colorRect.sizeDelta = new Vector2(20, 20);
                colorRect.anchoredPosition = new Vector2(x - 100, y);

                // 创建图例文本
                GameObject textObj = Instantiate(m_labelPrefab, legendItem.transform);
                TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "精确度";
                    text.color = Color.black;
                    textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x - 50, y);
                }

                m_chartObjects.Add(legendItem);
            }
        }

        #endregion
    }
}