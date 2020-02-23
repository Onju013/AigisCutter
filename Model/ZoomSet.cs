using System;

namespace AigisCutter.Model
{
    public struct ZoomSet : IEquatable<ZoomSet>
    {
        public double Zoom { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string ZoomP => Zoom + "%";

        public ZoomSet(
            double zoom,
            int width,
            int height)
        {
            Zoom = zoom;
            Width = width;
            Height = height;
        }

        public ZoomSet(
            double zoom)
            : this(zoom, (int)(960 * zoom / 100), (int)(640 * zoom / 100))
        {
        }

        public bool Equals(ZoomSet other)
        {
            if(ReferenceEquals(this, other))
                return true;

            return Zoom == other.Zoom;
        }
    }
}