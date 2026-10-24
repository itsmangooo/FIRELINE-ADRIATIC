using System;

namespace Fireline.Simulation
{
    /// <summary>Double-precision right-handed vector; flight uses east/right X, up Y and north/forward Z, in SI units.</summary>
    [Serializable]
    public struct FlightVector
    {
        public double X, Y, Z;
        /// <summary>Store three scalar components; boundary validation belongs to FlightDynamics.</summary>
        public FlightVector(double x, double y, double z) { X = x; Y = y; Z = z; }
        /// <summary>Euclidean magnitude, in the same units as the components.</summary>
        public double Length => Math.Sqrt(Dot(this, this));
        /// <summary>Add vectors expressed in the same frame and units.</summary>
        public static FlightVector operator +(FlightVector a, FlightVector b) => new FlightVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        /// <summary>Subtract vectors in the same frame, including world velocity minus wind.</summary>
        public static FlightVector operator -(FlightVector a, FlightVector b) => new FlightVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        /// <summary>Scale a vector by a scalar, without changing its coordinate frame.</summary>
        public static FlightVector operator *(FlightVector a, double b) => new FlightVector(a.X * b, a.Y * b, a.Z * b);
        /// <summary>Scalar product; used for speed squared and force-power checks.</summary>
        public static double Dot(FlightVector a, FlightVector b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        /// <summary>Right-handed cross product, used for lift direction, lever moments and gyroscopic torque.</summary>
        public static FlightVector Cross(FlightVector a, FlightVector b) => new FlightVector(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        /// <summary>Require finite components within an inclusive magnitude bound before accepting external state.</summary>
        public void Validate(double maximum, string name)
        {
            SimMath.Range(X, -maximum, maximum, name); SimMath.Range(Y, -maximum, maximum, name); SimMath.Range(Z, -maximum, maximum, name);
            if (Length > maximum) throw new ArgumentOutOfRangeException(name);
        }
    }

    /// <summary>Unit quaternion mapping body axes into world axes; no Euler-angle integration or Unity dependency.</summary>
    [Serializable]
    public struct FlightRotation
    {
        public double X, Y, Z, W;
        /// <summary>Store quaternion components; callers validate unit length before rotating physical state.</summary>
        public FlightRotation(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }
        /// <summary>No body-to-world rotation.</summary>
        public static FlightRotation Identity => new FlightRotation(0, 0, 0, 1);
        /// <summary>Squared quaternion norm; unit rotations have value one.</summary>
        public double NormSquared => X * X + Y * Y + Z * Z + W * W;
        /// <summary>Construct a finite rotation from an axis-times-angle vector in radians, including the zero-angle limit.</summary>
        public static FlightRotation FromRotationVector(FlightVector radians)
        {
            radians.Validate(100, nameof(radians)); double angle = radians.Length;
            double scale = angle < 1e-12 ? 0.5 : Math.Sin(angle / 2) / angle;
            return new FlightRotation(radians.X * scale, radians.Y * scale, radians.Z * scale, Math.Cos(angle / 2));
        }
        /// <summary>Compose rotations; a * b applies body-local b before a.</summary>
        public static FlightRotation operator *(FlightRotation a, FlightRotation b) => new FlightRotation(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
        /// <summary>Rotate a body vector into world space using this unit quaternion.</summary>
        public FlightVector Rotate(FlightVector vector)
        {
            var imaginary = new FlightVector(X, Y, Z); var cross = FlightVector.Cross(imaginary, vector) * 2;
            return vector + cross * W + FlightVector.Cross(imaginary, cross);
        }
        /// <summary>Rotate world coordinates into body space using the conjugate of this unit quaternion.</summary>
        public FlightVector InverseRotate(FlightVector vector) => new FlightRotation(-X, -Y, -Z, W).Rotate(vector);
        /// <summary>Remove accumulated roundoff after one quaternion composition; reject zero/nonfinite rotations.</summary>
        public FlightRotation Normalized()
        {
            double length = Math.Sqrt(NormSquared); SimMath.Range(length, 1e-9, 2, nameof(length));
            return new FlightRotation(X / length, Y / length, Z / length, W / length);
        }
        /// <summary>Reject nonunit or nonfinite external attitudes rather than silently repairing an invalid aircraft state.</summary>
        public void Validate() => SimMath.Range(NormSquared, 1 - 1e-8, 1 + 1e-8, "attitude norm");
    }
}
