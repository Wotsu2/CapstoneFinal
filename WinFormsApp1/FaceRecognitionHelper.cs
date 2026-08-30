using System;
using System.Drawing;
using FaceONNX;

namespace WinFormsApp1
{
    public class FaceRecognitionHelper : IDisposable
    {
        private readonly FaceDetector faceDetector;
        private readonly FaceEmbedder faceEmbedder;

        public FaceRecognitionHelper()
        {
            // Naka-embed na ang mga model sa loob ng FaceONNX package,
            // kaya parameterless na lang ang constructors.
            faceDetector = new FaceDetector();
            faceEmbedder = new FaceEmbedder();
        }

        // Kunin yung "face embedding" mula sa isang picture
        public float[] GetEmbedding(Bitmap image)
        {
            var faces = faceDetector.Forward(image);
            if (faces == null || faces.Length == 0)
                return null; // walang nakitang mukha

            Rectangle rect = faces[0].Rectangle;

            // Siguraduhin na hindi lumabas sa hangganan ng picture
            Rectangle safeRect = Rectangle.Intersect(rect, new Rectangle(0, 0, image.Width, image.Height));
            if (safeRect.Width <= 0 || safeRect.Height <= 0)
                return null;

            using (Bitmap cropped = image.Clone(safeRect, image.PixelFormat))
            using (Bitmap resized = new Bitmap(cropped, new Size(112, 112)))
            {
                return faceEmbedder.Forward(resized);
            }
        }

        // Sariling cosine similarity function (0 to 1, mas mataas = mas magkatugma)
        public float CompareFaces(float[] embedding1, float[] embedding2)
        {
            if (embedding1 == null || embedding2 == null || embedding1.Length != embedding2.Length)
                return -1f;

            float dot = 0f, mag1 = 0f, mag2 = 0f;
            for (int i = 0; i < embedding1.Length; i++)
            {
                dot += embedding1[i] * embedding2[i];
                mag1 += embedding1[i] * embedding1[i];
                mag2 += embedding2[i] * embedding2[i];
            }

            if (mag1 == 0 || mag2 == 0)
                return -1f;

            return dot / ((float)Math.Sqrt(mag1) * (float)Math.Sqrt(mag2));
        }

        public void Dispose()
        {
            faceDetector?.Dispose();
            faceEmbedder?.Dispose();
        }
    }
}