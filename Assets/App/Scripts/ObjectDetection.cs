// <copyright file="ObjectDetection.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

/// Script for CustomVision's exported object detection model.

namespace CustomVision
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
#if !UNITY_EDITOR
    using System.Threading.Tasks;
    using Windows.AI.MachineLearning;
    using Windows.Media;
    using Windows.Storage;
#endif

    public sealed class BoundingBox
    {
        public BoundingBox(float left, float top, float width, float height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }

        public float Left { get; private set; }
        public float Top { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
    }

    public sealed class PredictionModel
    {
        public PredictionModel(float probability, string tagName, BoundingBox boundingBox)
        {
            this.Probability = probability;
            this.TagName = tagName;
            this.BoundingBox = boundingBox;
        }

        public float Probability { get; private set; }
        public string TagName { get; private set; }
        public BoundingBox BoundingBox { get; private set; }
    }

    public class ObjectDetection
    {
        private static readonly float[] Anchors = new float[] { 0.573f, 0.677f, 1.87f, 2.06f, 3.34f, 5.47f, 7.88f, 3.53f, 9.77f, 9.17f };

        private readonly IList<string> labels;
        private readonly int maxDetections;
        private readonly float probabilityThreshold;
        private readonly float iouThreshold;
#if !UNITY_EDITOR
        private LearningModel model;
        private LearningModelSession session;
#endif
        public ObjectDetection(IList<string> labels, int maxDetections = 20, float probabilityThreshold = 0.1f, float iouThreshold = 0.45f)
        {
            this.labels = labels;
            this.maxDetections = maxDetections;
            this.probabilityThreshold = probabilityThreshold;
            this.iouThreshold = iouThreshold;
        }

#if !UNITY_EDITOR
        public async Task Init(string fileName)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(fileName));
            await Init(file);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="file">The ONNX file</param>
        public async Task Init(StorageFile file)
        {
            this.model = await LearningModel.LoadFromStorageFileAsync(file);
            this.session = new LearningModelSession(this.model);

            Debug.Assert(this.model.InputFeatures.Count == 1, "The number of input must be 1");
            Debug.Assert(this.model.OutputFeatures.Count == 1, "The number of output must be 1");
        }


        /// <summary>
        /// Detect objects from the given image.
        /// The input image must be 416x416.
        /// </summary>
        public async Task<IList<PredictionModel>> PredictImageAsync(VideoFrame image)
        {
            var imageFeature = ImageFeatureValue.CreateFromVideoFrame(image);
            var bindings = new LearningModelBinding(this.session);
            bindings.Bind("data", imageFeature);
            var result = await this.session.EvaluateAsync(bindings, "");
            return Postprocess(result.Outputs["model_outputs0"] as TensorFloat);
        }
#endif

        private static float Logistic(float x)
        {
            if (x > 0)
            {
                return (float)(1 / (1 + Math.Exp(-x)));
            }
            else
            {
                var e = Math.Exp(x);
                return (float)(e / (1 + e));
            }
        }

        /// <summary>
        /// Calculate Intersection over Union (IOU) for the given 2 bounding boxes.
        /// </summary>
        private static float CalculateIOU(BoundingBox box0, BoundingBox box1)
        {
            var x1 = Math.Max(box0.Left, box1.Left);
            var y1 = Math.Max(box0.Top, box1.Top);
            var x2 = Math.Min(box0.Left + box0.Width, box1.Left + box1.Width);
            var y2 = Math.Min(box0.Top + box0.Height, box1.Top + box1.Height);
            var w = Math.Max(0, x2 - x1);
            var h = Math.Max(0, y2 - y1);

            return w * h / ((box0.Width * box0.Height) + (box1.Width * box1.Height) - (w * h));
        }

#if !UNITY_EDITOR
        /// <summary>
        /// Extract bounding boxes and their probabilities from the prediction output.
        /// </summary>
        private ExtractedBoxes ExtractBoxes(TensorFloat predictionOutput, float[] anchors)
        {
            var shape = predictionOutput.Shape;
            Debug.Assert(shape.Count == 4, "The model output has unexpected shape");
            Debug.Assert(shape[0] == 1, "The batch size must be 1");

            IReadOnlyList<float> outputs = predictionOutput.GetAsVectorView();

            var numAnchor = anchors.Length / 2;
            var channels = shape[1];
            var height = shape[2];
            var width = shape[3];

            Debug.Assert(channels % numAnchor == 0);
            var numClass = (channels / numAnchor) - 5;

            Debug.Assert(numClass == this.labels.Count);

            var boxes = new List<BoundingBox>();
            var probs = new List<float[]>();
            for (int gridY = 0; gridY < height; gridY++)
            {
                for (int gridX = 0; gridX < width; gridX++)
                {
                    int offset = 0;
                    int stride = (int)(height * width);
                    int baseOffset = gridX + gridY * (int)width;

                    for (int i = 0; i < numAnchor; i++)
                    {
                        var x = (Logistic(outputs[baseOffset + (offset++ * stride)]) + gridX) / width;
                        var y = (Logistic(outputs[baseOffset + (offset++ * stride)]) + gridY) / height;
                        var w = (float)Math.Exp(outputs[baseOffset + (offset++ * stride)]) * anchors[i * 2] / width;
                        var h = (float)Math.Exp(outputs[baseOffset + (offset++ * stride)]) * anchors[i * 2 + 1] / height;

                        x = x - (w / 2);
                        y = y - (h / 2);

                        var objectness = Logistic(outputs[baseOffset + (offset++ * stride)]);

                        var classProbabilities = new float[numClass];
                        for (int j = 0; j < numClass; j++)
                        {
                            classProbabilities[j] = outputs[baseOffset + (offset++ * stride)];
                        }
                        var max = classProbabilities.Max();
                        for (int j = 0; j < numClass; j++)
                        {
                            classProbabilities[j] = (float)Math.Exp(classProbabilities[j] - max);
                        }
                        var sum = classProbabilities.Sum();
                        for (int j = 0; j < numClass; j++)
                        {
                            classProbabilities[j] *= objectness / sum;
                        }

                        if (classProbabilities.Max() > this.probabilityThreshold)
                        {
                            boxes.Add(new BoundingBox(x, y, w, h));
                            probs.Add(classProbabilities);
                        }
                    }
                    Debug.Assert(offset == channels);
                }
            }

            Debug.Assert(boxes.Count == probs.Count);
            return new ExtractedBoxes(boxes, probs);
        }
#endif

        /// <summary>
        /// Remove overlapping predictions and return top-n predictions.
        /// </summary>
        private IList<PredictionModel> SuppressNonMaximum(IList<BoundingBox> boxes, IList<float[]> probs)
        {
            var predictions = new List<PredictionModel>();
            if (probs.Any())
            {
                var maxProbs = probs.Select(x => x.Max()).ToArray();

                while (predictions.Count < this.maxDetections)
                {
                    var max = maxProbs.Max();
                    if (max < this.probabilityThreshold)
                    {
                        break;
                    }

                    var index = Array.IndexOf(maxProbs, max);
                    var maxClass = Array.IndexOf(probs[index], max);

                    predictions.Add(new PredictionModel(max, this.labels[maxClass], boxes[index]));

                    for (int i = 0; i < boxes.Count; i++)
                    {
                        if (CalculateIOU(boxes[index], boxes[i]) > this.iouThreshold)
                        {
                            probs[i][maxClass] = 0;
                            maxProbs[i] = probs[i].Max();
                        }
                    }
                }
            }

            return predictions;
        }

#if !UNITY_EDITOR
        private IList<PredictionModel> Postprocess(TensorFloat predictionOutputs)
        {
            var extractedBoxes = this.ExtractBoxes(predictionOutputs, ObjectDetection.Anchors);
            return this.SuppressNonMaximum(extractedBoxes.Boxes, extractedBoxes.Probabilities);
        }
#endif

        internal class ExtractedBoxes
        {
            public IList<BoundingBox> Boxes { get; private set; }
            public IList<float[]> Probabilities { get; private set; }

            public ExtractedBoxes(IList<BoundingBox> boxes, IList<float[]> probs)
            {
                Boxes = boxes;
                Probabilities = probs;
            }
        }

    }
}
