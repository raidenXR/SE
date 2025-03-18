const std = @import("std");
const PI = std.math.pi;
const EPSILON = std.math.e;

pub const Vector2 = @Vector(2, f32);
pub const Vector3 = @Vector(3, f32);
pub const Vector4 = @Vector(4, f32);

pub const Matrix3x2 = [6]f32;
pub const Matrix4x4 = [16]f32;
// pub const Matrix3x2 = @Vector(6, f32);
// pub const Matrix4x4 = @Vector(16, f32);

// pub const Quaternion = [4]f32;
pub const Quaternion = @Vector(4, f32);
pub const Plane = [4]f32;

pub const vec2 = struct 
{
    /// Returns the vector (0,0).
    pub const Zero = Vector2{0.0, 0.0};

    /// Returns the vector (1,1).
    pub const One = Vector2{1.0, 1.0};
    
    /// Returns the vector (1,0).
    pub const UnitX = Vector2{1.0, 0.0};        

    /// Returns the vector (0,1).
    pub const UnitY = Vector2{0.0, 1.0};

    // ###################################################
    // Intrisics
    // ###################################################
    pub fn dot (a:Vector2, b:Vector2) f32
    {
        return @reduce(.Add, a + b);        
    }

    pub fn equal(a:Vector2, b:Vector2) bool
    {
        return @reduce(.And, a == b);
    }
    
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
    pub fn min (a:Vector2, b:Vector2) Vector2
    {
        return @select(f32, a < b, a, b);    
    }
    
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors.
    pub fn max (a:Vector2, b:Vector2) Vector2
    {
        return @select(f32, a > b, a, b);    
    }
    
    /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
    pub fn abs (v:Vector2) Vector2
    {
        return @abs (v);
    }

    /// Returns a vector whose elements are the square root of each of the source vector's elements.
    pub fn sqrt (v:Vector2) Vector2
    {
        return @sqrt (v);
    }

    // ###################################################
    // Functions
    // ###################################################

    /// Returns the length of the vector.
    pub fn length (v:Vector2) f32
    {
        const ls = dot(v,v);
        return @sqrt (ls);
    }

    /// Returns the length of the vector squared. This operation is cheaper than Length().
    pub fn lengthSquared (v:Vector2) f32
    {
        return dot(v,v);
    }

    /// Returns the Euclidean distance between the two given points.
    pub fn distance (a:Vector2, b:Vector2) f32
    {
        const difference = a - b;
        const ls = dot (difference, difference);        
        return @sqrt (ls);
    }

    /// Returns the Euclidean distance squared between the two given points.
    pub fn distanceSquared (a:Vector2, b:Vector2) f32
    {
        const difference = a - b;
        return dot (difference, difference);
    }

    /// Returns a vector with the same direction as the given vector, but with a length of 1.
    pub fn normalize (v:Vector2) Vector2
    {
        const len: Vector2 = @splat(length(v));
        return v / len;
    }

    /// Returns the reflection of a vector off a surface that has the specified normal.
    pub fn reflect (vector:Vector3, normal:Vector3) Vector2
    {
        const _dot = dot(vector, normal);
        const temp = normal * _dot * @as(Vector2, @splat(2.0));
        return vector - temp;
    }
    
    /// Restricts a vector between a min and max value.
    pub fn clamp (v:Vector2, _min:Vector2, _max:Vector2) Vector2
    {
        var x = v[0];
        x = if (x > _max[0]) _max[0] else x;
        x = if (x < _min[0]) _min[0] else x;
        
        var y = v[1];
        y = if (y > _max[0]) _max[0] else y;
        y = if (y < _min[0]) _min[0] else y;
        
        return Vector2{x, y};
    }
    
    /// Linearly interpolates between two vectors based on the given weighting
    pub fn lerp (v1:Vector2, v2:Vector2, amount:f32) Vector2
    {
        return Vector2{
            v1[0] + (v2[0] - v1[0]) * amount,
            v1[1] + (v2[1] - v1[1]) * amount,
        };
    }
    
    /// Transforms a vector by the given matrix.
    pub fn transform (position:Vector3, m:Matrix3x2) Vector2
    {
        return Vector3{
            position[0] * m[0] + position[1] * m[2] + m[4],
            position[0] * m[1] + position[1] * m[3] + m[5],
        };
    }
    
    /// Transforms a vector normal by the given matrix.
    pub fn transformNormal (normal:Vector2, m:Matrix4x4) Vector2
    {
        return Vector3{
            normal[0] * m[0] + normal[1] * m[4],
            normal[0] * m[1] + normal[1] * m[5],
        };        
    }
    
    /// Transforms a vector by the given Quaternion rotation value.
    pub fn rotate (v:Vector2, rotation:Quaternion) Vector2
    {
        const x2 = rotation[0] + rotation[0];
        const y2 = rotation[1] + rotation[1];
        const z2 = rotation[2] + rotation[2];

        const wz2 = rotation[3] * z2;
        const xx2 = rotation[0] * x2;
        const xy2 = rotation[0] * y2;
        const yy2 = rotation[1] * y2;
        const zz2 = rotation[2] * z2;

        return Vector2{
            v[0] * (1.0 - yy2 - zz2) + v[1] * (xy2 - wz2),
            v[0] * (xy2 + wz2) + v[1] * (1.0 - xx2 - zz2),
        };
    }
};

pub const vec3 = struct 
{
    /// Returns the vector (0,0,0).
    pub const Zero = Vector3{0.0, 0.0, 0.0};

    /// Returns the vector (1,1,1).
    pub const One = Vector3{1.0, 1.0, 1.0};

    /// Returns the vector (1,0,0).
    pub const UnitX = Vector3{1.0, 0.0, 0.0};

    /// Returns the vector (0,1,0).
    pub const UnitY = Vector3{0.0, 1.0, 0.0};

    /// Returns the vector (0,0,1).
    pub const UnitZ = Vector3{0.0, 0.0, 1.0};

    // ###################################################
    // Intrisics
    // ###################################################
    pub fn dot (a:Vector3, b:Vector3) f32
    {
        return @reduce(.Add, a + b);        
    }

    pub fn equal(a:Vector3, b:Vector3) bool
    {
        return @reduce(.And, a == b);
    }
    
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
    pub fn min (a:Vector3, b:Vector3) Vector3
    {
        return @select(f32, a < b, a, b);    
    }
    
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors.
    pub fn max (a:Vector3, b:Vector3) Vector3
    {
        return @select(f32, a > b, a, b);    
    }
    
    /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
    pub fn abs (v:Vector3) Vector3
    {
        return @abs (v);
    }

    /// Returns a vector whose elements are the square root of each of the source vector's elements.
    pub fn sqrt (v:Vector3) Vector3
    {
        return @sqrt (v);
    }

    // ###################################################
    // Functions
    // ###################################################

    /// Returns the length of the vector.
    pub fn length (v:Vector3) f32
    {
        const ls = dot(v,v);
        return @sqrt (ls);
    }

    /// Returns the length of the vector squared. This operation is cheaper than Length().
    pub fn lengthSquared (v:Vector3) f32
    {
        return dot(v,v);
    }

    /// Returns the Euclidean distance between the two given points.
    pub fn distance (a:Vector3, b:Vector3) f32
    {
        const difference = a - b;
        const ls = dot (difference, difference);        
        return @sqrt (ls);
    }

    /// Returns the Euclidean distance squared between the two given points.
    pub fn distanceSquared (a:Vector3, b:Vector3) f32
    {
        const difference = a - b;
        return dot (difference, difference);
    }

    /// Returns a vector with the same direction as the given vector, but with a length of 1.
    pub fn normalize (v:Vector3) Vector3
    {
        const len: Vector3 = @splat(length(v));
        return v / len;
    }

    /// Computes the cross product of two vectors.
    pub fn cross (a:Vector3, b:Vector3) Vector3
    {
        return Vector3{
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0],
        };
    }

    /// Returns the reflection of a vector off a surface that has the specified normal.
    pub fn reflect (vector:Vector3, normal:Vector3) Vector3
    {
        const _dot = dot(vector, normal);
        const temp = normal * _dot * @as(Vector3, @splat(2.0));
        return vector - temp;
    }
    
    /// Restricts a vector between a min and max value.
    pub fn clamp (v:Vector3, _min:Vector3, _max:Vector3) Vector3
    {
        var x = v[0];
        x = if (x > _max[0]) _max[0] else x;
        x = if (x < _min[0]) _min[0] else x;
        
        var y = v[1];
        y = if (y > _max[0]) _max[0] else y;
        y = if (y < _min[0]) _min[0] else y;
        
        var z = v[2];
        z = if (z > _max[0]) _max[0] else z;
        z = if (z < _min[0]) _min[0] else z;

        return Vector3{x, y, z};
    }
    
    /// Linearly interpolates between two vectors based on the given weighting
    pub fn lerp (v1:Vector3, v2:Vector3, amount:f32) Vector3
    {
        const firstInfluence  = v1 * @as(Vector3, @splat(1.0 - amount));
        const secondInfluence = v2 * @as(Vector3, @splat(amount));
        return firstInfluence - secondInfluence;
    }
    
    /// Transforms a vector by the given matrix.
    pub fn transform (position:Vector3, m:Matrix4x4) Vector3
    {
        return Vector3{
            position[0] * m[0] + position[1] * m[4] + position[2] * m[8] + m[12],
            position[0] * m[1] + position[1] * m[5] + position[2] * m[9] + m[13],
            position[0] * m[2] + position[1] * m[6] + position[2] * m[10] + m[14],
        };
    }
    
    /// Transforms a vector normal by the given matrix.
    pub fn transformNormal (normal:Vector3, m:Matrix4x4) Vector3
    {
        return Vector3{
            normal[0] * m[0] + normal[1] * m[4] + normal[2] * m[8],
            normal[0] * m[1] + normal[1] * m[5] + normal[2] * m[9],
            normal[0] * m[2] + normal[1] * m[6] + normal[2] * m[10],
        };        
    }
    
    /// Transforms a vector by the given Quaternion rotation value.
    pub fn rotate (v:Vector3, rotation:Quaternion) Vector3
    {
        const x2 = rotation[0] + rotation[0];
        const y2 = rotation[1] + rotation[1];
        const z2 = rotation[2] + rotation[2];

        const wx2 = rotation[3] * x2;
        const wy2 = rotation[3] * y2;
        const wz2 = rotation[3] * z2;
        const xx2 = rotation[0] * x2;
        const xy2 = rotation[0] * y2;
        const xz2 = rotation[0] * z2;
        const yy2 = rotation[1] * y2;
        const yz2 = rotation[1] * z2;
        const zz2 = rotation[2] * z2;

        return Vector3{
            v[0] * (1.0 - yy2 - zz2) + v[1] * (xy2 - wz2) + v[2] * (xz2 + wy2),
            v[0] * (xy2 + wz2) + v[1] * (1.0 - xx2 - zz2) + v[2] * (yz2 - wx2),
            v[0] * (xz2 - wy2) + v[1] * (yz2 + wx2) + v[2] * (1.0 - xx2 + yy2),
        };
    }
};


pub const vec4 = struct 
{
    /// Returns the vector (0,0,0,0).
    pub const Zero = Vector4{0.0, 0.0, 0.0, 0.0};

    /// Returns the vector (1,1,1,1).
    pub const One = Vector4{1.0, 1.0, 1.0, 1.0};

    /// Returns the vector (1,0,0,0).
    pub const UnitX = Vector4{1.0, 0.0, 0.0, 0.0};

    /// Returns the vector (0,1,0,0).
    pub const UnitY = Vector4{0.0, 1.0, 0.0, 0.0};

    /// Returns the vector (0,0,1,0).
    pub const UnitZ = Vector4{0.0, 0.0, 1.0, 0.0};

    /// Returns the vector (0,0,0,1).
    pub const UnitW = Vector4{0.0, 0.0, 0.0, 1.0};

    // ###################################################
    // Intrisics
    // ###################################################
    pub fn dot (a:Vector4, b:Vector4) f32
    {
        return @reduce(.Add, a + b);        
    }

    pub fn equal (a:Vector4, b:Vector4) bool
    {
        return @reduce(.And, a == b);
    }
    
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
    pub fn min (a:Vector4, b:Vector4) Vector4
    {
        return @select(f32, a < b, a, b);    
    }
    
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors.
    pub fn max (a:Vector4, b:Vector4) Vector4
    {
        return @select(f32, a > b, a, b);    
    }
    
    /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
    pub fn abs (v:Vector4) Vector4
    {
        return @abs (v);
    }

    /// Returns a vector whose elements are the square root of each of the source vector's elements.
    pub fn sqrt (v:Vector4) Vector4
    {
        return @sqrt (v);
    }

    // ###################################################
    // Functions
    // ###################################################

    /// Returns the length of the vector.
    pub fn length (v:Vector4) f32
    {
        const ls = dot(v,v);
        return @sqrt (ls);
    }

    /// Returns the length of the vector squared. This operation is cheaper than Length().
    pub fn lengthSquared (v:Vector4) f32
    {
        return dot(v,v);
    }

    /// Returns the Euclidean distance between the two given points.
    pub fn distance (a:Vector4, b:Vector4) f32
    {
        const difference = a - b;
        const ls = dot (difference, difference);        
        return @sqrt (ls);
    }

    /// Returns the Euclidean distance squared between the two given points.
    pub fn distanceSquared (a:Vector4, b:Vector4) f32
    {
        const difference = a - b;
        return dot (difference, difference);
    }

    /// Returns a vector with the same direction as the given vector, but with a length of 1.
    pub fn normalize (v:Vector4) Vector4
    {
        const len: Vector4 = @splat(length(v));
        return v / len;
    }
    
    /// Restricts a vector between a min and max value.
    pub fn clamp (v:Vector4, _min:Vector4, _max:Vector4) Vector4
    {
        var x = v[0];
        x = if (x > _max[0]) _max[0] else x;
        x = if (x < _min[0]) _min[0] else x;
        
        var y = v[1];
        y = if (y > _max[0]) _max[0] else y;
        y = if (y < _min[0]) _min[0] else y;
        
        var z = v[2];
        z = if (z > _max[0]) _max[0] else z;
        z = if (z < _min[0]) _min[0] else z;

        var w = v[2];
        w = if (w > _max[0]) _max[0] else w;
        w = if (w < _min[0]) _min[0] else w;

        return Vector3{x, y, z, w};
    }
    
    /// Linearly interpolates between two vectors based on the given weighting
    pub fn lerp (v1:Vector4, v2:Vector4, amount:f32) Vector4
    {
        return Vector4{
            v1[0] + (v2[0] - v1[0]) * amount,
            v1[1] + (v2[1] - v1[1]) * amount,
            v1[2] + (v2[2] - v1[2]) * amount,
            v1[3] + (v2[3] - v1[3]) * amount,
        };
    }
    
    /// Transforms a vector by the given matrix.
    pub fn transform (v:Vector4, m:Matrix4x4) Vector4
    {
        return Vector4{
            v[0] * m[0] + v[1] * m[4] + v[2] * m[8] + v[3] * m[12],
            v[0] * m[1] + v[1] * m[5] + v[2] * m[9] + v[3] * m[13],
            v[0] * m[2] + v[1] * m[6] + v[2] * m[10] + v[3] * m[14],
            v[0] * m[3] + v[1] * m[7] + v[2] * m[11] + v[3] * m[15],
        };
    }
    
    /// Transforms a vector normal by the given matrix.
    pub fn transformNormal (normal:Vector3, m:Matrix4x4) Vector3
    {
        return Vector3{
            normal[0] * m[0] + normal[1] * m[4] + normal[2] * m[8],
            normal[0] * m[1] + normal[1] * m[5] + normal[2] * m[9],
            normal[0] * m[2] + normal[1] * m[6] + normal[2] * m[10],
        };        
    }
    
    /// Transforms a vector by the given Quaternion rotation value.
    pub fn rotate (v:Vector4, rotation:Quaternion) Vector4
    {
        const x2 = rotation[0] + rotation[0];
        const y2 = rotation[1] + rotation[1];
        const z2 = rotation[2] + rotation[2];

        const wx2 = rotation[3] * x2;
        const wy2 = rotation[3] * y2;
        const wz2 = rotation[3] * z2;
        const xx2 = rotation[0] * x2;
        const xy2 = rotation[0] * y2;
        const xz2 = rotation[0] * z2;
        const yy2 = rotation[1] * y2;
        const yz2 = rotation[1] * z2;
        const zz2 = rotation[2] * z2;

        return Vector4{
            v[0] * (1.0 - yy2 - zz2) + v[1] * (xy2 - wz2) + v[2] * (xz2 + wy2),
            v[0] * (xy2 + wz2) + v[1] * (1.0 - xx2 - zz2) + v[2] * (yz2 - wx2),
            v[0] * (xz2 - wy2) + v[1] * (yz2 + wx2) + v[2] * (1.0 - xx2 + yy2),
            v[3],
        };
    }
};

pub const quaternion = struct
{
    /// Calculates the length of the Quaternion.
    pub fn length (q:Quaternion) f32
    {
        const ls = q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3];

        return @sqrt(ls);
    }

    /// Calculates the length squared of the Quaternion. This operation is cheaper than Length().
    pub fn lengthSquared (q:Quaternion) f32
    {
        return q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3];        
    }

    /// Divides each component of the Quaternion by the length of the Quaternion.
    pub fn normalize (q:Quaternion) Quaternion
    {
        const ls = q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3];
        
        const invNorm = 1.0 / @sqrt(ls);

        return Quaternion{
            q[0] * invNorm,
            q[1] * invNorm,
            q[2] * invNorm,
            q[3] * invNorm,
        };
    }

    /// Creates the conjugate of a specified Quaternion.
    pub fn conjugate (q:Quaternion) Quaternion
    {
        return Quaternion{-q[0], -q[1], -q[2], q[3]};
    }

    /// Returns the inverse of a Quaternion.
    pub fn inverse (q:Quaternion) Quaternion
    {
        const ls = q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3];        
        const invNorm = 1.0 / ls;
        
        return Quaternion{
            -q[0] * invNorm,
            -q[1] * invNorm,
            -q[2] * invNorm,
            q[3] * invNorm,
        };
    }

    /// Creates a Quaternion from a vector and an angle to rotate about the vector.
    pub fn createFromAxisAngle (axis:Vector3, angle:f32) Quaternion
    {
        const half_angle = angle * 0.5;
        const s = @sin(half_angle);
        const c = @cos(half_angle);

        return Quaternion{axis[0] * s, axis[1] * s, axis[2] * s, c};
    }

    /// Creates a new Quaternion from the given yaw, pitch, and roll, in radians.    
    pub fn createFromPitchYawRoll (yaw:f32, pitch:f32, roll:f32) Quaternion
    {
        const half_roll = roll * 0.5;
        const sr = @sin(half_roll);
        const cr = @sin(half_roll);

        const half_pitch = pitch * 0.5;
        const sp = @sin(half_pitch);
        const cp = @sin(half_pitch);

        const half_yaw = yaw * 0.5;
        const sy = @sin(half_yaw);
        const cy = @sin(half_yaw);

        return Quaternion{
            cy * sp * cr + sy * cp * sr,
            sy * cp * cr - cy * sp * sr,
            cy * cp * sr - sy * sp * cr,
            cy * cp * cr + sy * sp * sr,
        };
    }

    /// Creates a Quaternion from the given rotation matrix.
    pub fn createFromRotationMatrix (m:Matrix4x4) Quaternion
    {
        _ = m; // autofix
    
        noreturn;
    }

    /// Calculates the dot product of two Quaternions.
    pub fn dot (q1:Quaternion, q2:Quaternion) f32
    {
        return q1[0] * q2[0] + q1[1] * q1[1] + q1[2] * q2[2] + q1[3] * q2[3];
    }

    /// Interpolates between two quaternions, using spherical linear interpolation.
    pub fn slerp (q1:Quaternion, q2:Quaternion, amount:f32) Quaternion
    {
        _ = q1; // autofix
        _ = q2; // autofix
        _ = amount; // autofix
    
        noreturn;
    }

    ///  Linearly interpolates between two quaternions.
    pub fn lerp (q1:Quaternion, q2:Quaternion, amount:f32) Quaternion
    {
        _ = q1; // autofix
        _ = q2; // autofix
        _ = amount; // autofix
    
        noreturn;
    }

    /// Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.
    pub fn concatenate (q1:Quaternion, q2:Quaternion) Quaternion
    {
        // Concatenate rotation is actually q2 * q1 instead of q1 * q2.
        // So that's why value2 goes q1 and value1 goes q2.
        const q1x = q2[0];
        const q1y = q2[1];
        const q1z = q2[2];
        const q1w = q2[3];

        const q2x = q1[0];
        const q2y = q1[1];
        const q2z = q1[2];
        const q2w = q1[3];

        // cross(av, bv)
        const cx = q1y * q2z - q1z * q2y;
        const cy = q1z * q2x - q1x * q2z;
        const cz = q1x * q2y - q1y * q2x;

        const _dot = q1x * q2x + q1y * q2y + q1z * q2z;

        return Quaternion{
            q1x * q2w + q2x * q1w + cx,
            q1y * q2w + q2y * q1w + cy,
            q1z * q2w + q2z * q1w + cz,
            q1w * q2w - _dot,           
        };
    }
};

pub const plane = struct
{
    
};

pub const mat3x2 = struct
{
    /// Returns the multiplicative identity matrix.
    pub fn identity () Matrix3x2
    {
        return Matrix3x2{
            1.0, 0.0,
            0.0, 1.0,
            0.0, 0.0,
        };
    }

    /// Returns whether the matrix is the identity matrix.
    pub fn isIdentity (m:Matrix4x4) bool
    {
        const _identity = identity();
        var _is_identity = true;

        for (m, _identity) |b0, b1| _is_identity = _is_identity and b0 and b1;
        return _is_identity;
    }
    
    /// Creates a translation matrix.
    pub fn createTranslation (position:Vector2) Matrix3x2
    {
        return Matrix3x2{
            1.0, 0.0,
            0.0, 1.0,
            position[0], position[1],
        };       
    }

    /// Creates a scaling matrix.
    pub fn createScale (xscale:f32, yscale:f32) Matrix3x2
    {
        return Matrix3x2{
            xscale, 0.0,
            0.0, yscale,
            0.0, 0.0,
        };
    }
    
    /// Creates a scaling matrix with a center point.
    pub fn createScaleWithCenterPoint (xscale:f32, yscale:f32, center_point:Vector2) Matrix3x2
    {
        const tx = center_point[0] * (1.0 - xscale);
        const ty = center_point[1] * (1.0 - yscale);
        
        return Matrix3x2{
            xscale, 0.0,
            0.0, yscale,
            tx, ty,
        };
    }

    /// Creates a skew matrix from the given angles in radians.
    pub fn createSkew (radiansX:f32, radiansY:f32) Matrix3x2
    {
        const xtan = @tan(radiansX);
        const ytan = @tan(radiansY);

        return Matrix3x2{
            1.0, ytan,
            xtan, 1.0,
            0.0,  0.0,            
        };
    }

    /// Creates a skew matrix from the given angles in radians and a center point.
    pub fn createSkewFromCenterPoint (radiansX:f32, radiansY:f32, center_point:Vector2) Matrix3x2
    {
        const xtan = @tan(radiansX);
        const ytan = @tan(radiansY);

        const tx = -center_point[1] * xtan;
        const ty = -center_point[0] * ytan;

        return Matrix3x2{
            1.0, ytan,
            xtan, 1.0,
            tx, ty,            
        };
        
    }

    /// Creates a rotation matrix using the given rotation in radians.
    pub fn createRotation (radians:f32) Matrix3x2
    {
        _ = radians; // autofix
    
        noreturn;
    }

    /// Creates a rotation matrix using the given rotation in radians and a center point.
    pub fn createRotationFromCenterPoint (randians:f32, center_point:Vector2) Matrix3x2
    {
        _ = randians; // autofix
        _ = center_point; // autofix
    
        noreturn;
    }

    /// Calculates the determinant for this matrix. 
    pub fn deternminant (m:Matrix3x2) f32
    {
        return (m[0] * m[3]) - (m[2] * m[1]);
    }

    /// Attempts to invert the given matrix. If the operation succeeds, the inverted matrix is stored in the result parameter.
    pub fn invert (m:Matrix3x2, result:*Matrix3x2) bool
    {
        const det = (m[0] * m[3]) - (m[2] * m[1]);

        if (@abs(det) < EPSILON)
        {
            result.* = Matrix3x2{};
            return false;
        }

        const invDet = 1.0 / det;

        result.* = Matrix3x2{
            m[3] * invDet,
            -m[1] * invDet,
            -m[2] * invDet,
            m[0] * invDet,
            (m[2] * m[5] - m[4] * m[3]) * invDet,
            (m[4] * m[1] - m[0] * m[5]) * invDet,           
        };

        return true;            
    }

    /// Linearly interpolates between the corresponding values of two matrices.
    pub fn lerp (m1:Matrix3x2, m2:Matrix3x2, amount:f32) Matrix3x2
    {
        return Matrix3x2{
            // First row
            m1[0] + (m2[0] - m1[0]) * amount,
            m1[1] + (m2[1] - m1[1]) * amount,
            
            // Second row
            m1[2] + (m2[2] - m1[2]) * amount,
            m1[3] + (m2[3] - m1[3]) * amount,

            // Third row
            m1[4] + (m2[4] - m1[4]) * amount,
            m1[5] + (m2[5] - m1[5]) * amount,
        };
    }

    
    /// Returns a new matrix with the negated elements of the given matrix.
    pub fn negate (m:Matrix3x2) Matrix3x2
    {
        var res = Matrix3x2{};
        inline for (0..6) |i| res[i] = -m[i];

        return res;
    }

    /// Adds two matrices together.
    pub fn add (m1:Matrix3x2, m2:Matrix3x2) Matrix3x2
    {
        var res = Matrix3x2{};
        inline for (0..6) |i| res[i] = m1[i] + m2[i];

        return res;
    }

    /// Subtracts the second matrix from the first.
    pub fn subtract (m1:Matrix3x2, m2:Matrix3x2) Matrix3x2
    {
        var res = Matrix3x2{};
        inline for (0..6) |i| res[i] = m1[i] - m2[i];

        return res;
    }

    /// Multiplies a matrix by another matrix.
    pub fn multiply (m1:Matrix3x2, m2:Matrix3x2) Matrix3x2
    {
        return Matrix3x2{
            // First row
            m1[0] * m2[0] + m1[1] * m2[2],
            m1[0] * m2[1] + m1[1] * m2[3],

            // Second row
            m1[2] * m2[0] + m1[3] * m2[2],
            m1[2] * m2[1] + m1[3] * m2[3],

            // Third row
            m1[4] * m2[0] + m1[5] * m2[2] + m2[4],
            m1[4] * m2[1] + m1[5] * m2[3] + m2[5],
        };        
    }
};

pub const mat4x4 = struct 
{
    /// Returns the multiplicative identity matrix.
    pub fn identity () Matrix4x4
    {
        return Matrix4x4{
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0,
        };
    }

    /// Returns whether the matrix is the identity matrix.
    pub fn isIdentity (m:Matrix4x4) bool
    {
        const _identity = identity();
        var _is_identity = true;

        for (m, _identity) |b0, b1| _is_identity = _is_identity and b0 and b1;
        return _is_identity;
    }

    /// Creates a spherical billboard that rotates around a specified object position.
    pub fn createBillboard (object_position:Vector3, camera_position:Vector3, camera_upvector:Vector3, camera_forwardvector:Vector3) Matrix4x4
    {
        const epsilon = 1e-4;

        var zaxis = Vector3{
            object_position[0] - camera_position[0],
            object_position[1] - camera_position[1],
            object_position[2] - camera_position[2],
        };

        const norm  = vec3.lengthSquared (zaxis);

        if (norm < epsilon)
        {
            zaxis = -camera_forwardvector;
        } 
        else 
        {
            zaxis = zaxis * @as(Vector3, @splat(1.0 / @sqrt(norm)));
        }

        const xaxis = vec3.normalize (vec3.cross (camera_upvector, zaxis));
        const yaxis = vec3.cross (zaxis, xaxis);

        return Matrix4x4{
            xaxis[0],
            xaxis[1],
            xaxis[2],
            0.0,
            yaxis[0],
            yaxis[1],
            yaxis[2],
            0.0,
            zaxis[0],
            zaxis[1],
            zaxis[2],
            0.0,
            0.0, 0.0, 0.0, 0.0,
        };
    }

    /// Creates a cylindrical billboard that rotates around a specified axis.
    pub fn createConstrainedBillboard (object_position:Vector3, camera_position:Vector3, rotate_axis:Vector3, camera_forward_vector:Vector3, object_forward_vector:Vector3) Matrix4x4
    {
        const epsilon:   f32 = 1e-4;
        const min_angle: f32 = 1.0 - (0.1 * PI / 180.0);
        
        var face_dir = Vector3{
            object_position[0] - camera_position[0],
            object_position[1] - camera_position[1],
            object_position[2] - camera_position[2],
        };

        const norm = vec3.lengthSquared (face_dir);

        if (norm < epsilon)
        {
            face_dir = -camera_forward_vector;
        }
        else
        {
            face_dir = face_dir * @as(Vector3, @splat(1.0 / @sqrt(norm)));
        }

        const yaxis = rotate_axis;
        var xaxis: Vector3 = undefined;
        var zaxis: Vector3 = undefined;

        var _dot = vec3.dot (rotate_axis, face_dir);

        if (@abs (_dot) > min_angle)
        {
            zaxis = object_forward_vector;

            _dot = vec3.dot (rotate_axis, zaxis);

            if (@abs(_dot) > min_angle)
            {
                zaxis = if (@abs(rotate_axis[2]) > min_angle) Vector3{1.0, 0.0, 0.0} else Vector3{0.0, 0.0, -1.0};
            }

            xaxis = vec3.normalize (vec3.cross (rotate_axis, zaxis));
            zaxis = vec3.normalize (vec3.cross (xaxis, rotate_axis));
        }
        else
        {
            xaxis = vec3.normalize (vec3.cross (rotate_axis), face_dir);
            zaxis = vec3.normalize (vec3.cross (xaxis, yaxis));
        }

        return Matrix4x4{
            xaxis[0],
            xaxis[1],
            xaxis[2],
            0.0,
            yaxis[0],
            yaxis[1],
            yaxis[2],
            0.0,
            zaxis[0],
            zaxis[1],
            zaxis[2],
            0.0,
            0.0, 0.0, 0.0, 0.0,
        };
    }

    /// Creates a translation matrix.
    pub fn createTranslation (position:Vector3) Matrix4x4
    {
        return Matrix4x4{
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            position[0], position[1], position[2], 1.0,
        };       
    }
    
    /// Creates a scaling matrix.
    pub fn createScale (xscale:f32, yscale:f32, zscale:f32) Matrix4x4
    {
        return Matrix4x4{
            xscale, 0.0, 0.0, 0.0,
            0.0, yscale, 0.0, 0.0,
            0.0, 0.0, zscale, 0.0,
            0.0, 0.0, 0.0, 1.0,
        };
    }

    /// Creates a scaling matrix with a center point.
    pub fn createScaleWithCenterPoint (xscale:f32, yscale:f32, zscale:f32, center_point:Vector3) Matrix4x4
    {
        const tx = center_point[0] * (1.0 - xscale);
        const ty = center_point[1] * (1.0 - yscale);
        const tz = center_point[2] * (1.0 - zscale);
        
        return Matrix4x4{
            xscale, 0.0, 0.0, 0.0,
            0.0, yscale, 0.0, 0.0,
            0.0, 0.0, zscale, 0.0,
            tx, ty, tz, 1.0,
        };
    }

    /// Creates a matrix for rotating points around the X-axis.
    pub fn createRotationX (radians:f32) Matrix4x4
    {
        const c: f32 = @cos(radians);
        const s: f32 = @sin(radians);

        return Matrix4x4{
            1.0, 0.0, 0.0, 0.0,
            0.0, c, s, 0.0,
            0.0, -s, c, 0.0,
            0.0, 0.0, 0.0, 1.0,
        };
    }

    /// Creates a matrix for rotating points around the X-axis, from a center point.
    pub fn createRotationXFromCenterPoint (radians:f32, center_point:Vector3) Matrix4x4
    {
        const c: f32 = @cos(radians);
        const s: f32 = @sin(radians);

        const y = center_point[1] * (1.0 - c) + center_point[2] * s;
        const z = center_point[2] * (1.0 - c) + center_point[1] * s;

        return Matrix4x4{
            1.0, 0.0, 0.0, 0.0,
            0.0, c, s, 0.0,
            0.0, -s, c, 0.0,
            0.0, y, z, 1.0,
        };
    }
    
    /// Creates a matrix for rotating points around the Y-axis.
    pub fn createRotationY (radians:f32) Matrix4x4
    {
        const c: f32 = @cos(radians);
        const s: f32 = @sin(radians);

        return Matrix4x4{
            c, 0.0, -s, 0.0,
            0.0, 1.0, 0.0, 0.0,
            s, 0.0, c, 0.0,
            0.0, 0.0, 0.0, 1.0,
        };
    }

    /// Creates a matrix for rotating points around the Y-axis, from a center point.
    pub fn createRotationYFromCenterPoint (radians:f32, center_point:Vector3) Matrix4x4
    {
        const c: f32 = @cos(radians);
        const s: f32 = @sin(radians);

        const x = center_point[0] * (1.0 - c) - center_point[2] * s;
        const z = center_point[2] * (1.0 - c) + center_point[0] * s;

        return Matrix4x4{
            c, 0.0, -s, 0.0,
            0.0, 1.0, 0.0, 0.0,
            s, 0.0, c, 0.0,
            x, 0.0, z, 1.0,
        };
    }

    /// Creates a matrix for rotating points around the Z-axis.
    pub fn createRotationZ (radians:f32) Matrix4x4
    {
        const c: f32 = @cos(radians);
        const s: f32 = @sin(radians);

        return Matrix4x4{
            c, s, 0.0, 0.0,
            -s, c, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0,
        };
    }

    /// Creates a matrix for rotating points around the Z-axis, from a center point.
    pub fn createRotationZFromCenterPoint (radians:f32, center_point:Vector3) Matrix4x4
    {
        const c: f32 = @cos(radians);
        const s: f32 = @sin(radians);

        const x = center_point[0] * (1.0 - c) + center_point[1] * s;
        const y = center_point[1] * (1.0 - c) - center_point[0] * s;

        return Matrix4x4{
            c, s, 0.0, 0.0,
            -s, c, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            x, y, 0.0, 1.0,
        };
    }
    
    /// Creates a matrix that rotates around an arbitrary vector.
    pub fn createFromAxisAngle (axis:Vector3, angle:f32) Matrix4x4
    {
        const x = axis[0];
        const y = axis[1];
        const z = axis[2];

        const sa = @sin(angle);
        const ca = @cos(angle);
        
        const xx = x * x;
        const yy = y * y;
        const zz = z * z;
        const xy = x * y;
        const xz = x * z;
        const yz = y * z;

        return Matrix4x4{
            xx + ca * (1.0 - xx),
            xy - ca * xy + sa * z,
            xz - ca * xz - sa * y,
            0.0,
            xy - ca * xy - sa * z,
            yy + ca * (1.0 - yy),
            yz - ca * yz + sa * x,
            0.0,
            xz - ca * xz + sa * y,
            yz - ca * yz - sa * x,
            zz + ca * (1.0 - zz),
            0.0,
            0.0, 0.0, 0.0, 1.0,            
        };
    }

    /// Creates a perspective projection matrix based on a field of view, aspect ratio, and near and far view plane distances. 
    pub fn createPerspectiveFieldOfView (field_of_view:f32, aspect_ration:f32, near_plane_distance:f32, far_plane_distance:f32) Matrix4x4
    {
        if (field_of_view <= 0.0 or field_of_view >= PI) @panic ("field_of_view out of range\n");
        if (near_plane_distance <= 0.0) @panic ("near_plane_distance out of range\n");
        if (far_plane_distance <= 0.0) @panic ("far_plane_distance out of range\n");
        if (near_plane_distance >= far_plane_distance) @panic ("near_plane_distance out of range\n");

        const yscale = 1.0 / @tan(field_of_view * 0.5);
        const xscale = yscale / aspect_ration;

        return Matrix4x4{
            xscale, 0.0, 0.0, 0.0,
            0.0, yscale, 0.0, 0.0,
            0.0, 0.0, (far_plane_distance / (near_plane_distance - far_plane_distance)), -1.0,
            0.0, 0.0, (near_plane_distance * far_plane_distance / (near_plane_distance - far_plane_distance)), 0.0,
        };
    }

    /// Creates a perspective projection matrix from the given view volume dimensions.
    pub fn createPerspective (width:f32, height:f32, near_plane_distance:f32, far_plane_distance:f32) Matrix4x4
    {
        if (near_plane_distance <= 0.0) @panic ("near_plane_distance out of range\n");
        if (far_plane_distance <= 0.0) @panic ("far_plane_distance out of range\n");
        if (near_plane_distance >= far_plane_distance) @panic ("near_plane_distance out of range\n");
        
        return Matrix4x4{
            (2.0 * near_plane_distance / width), 0.0, 0.0, 0.0,
            0.0, (2.0 * near_plane_distance / height), 0.0, 0.0,
            0.0, 0.0, (far_plane_distance / (near_plane_distance - far_plane_distance)), -1.0,
            0.0, 0.0, (near_plane_distance * far_plane_distance / (near_plane_distance - far_plane_distance)), 0.0,
        };
    }

    /// Creates a customized, perspective projection matrix.
    pub fn createPerspectiveOffCenter (left:f32, right:f32, bottom:f32, top:f32, near_plane_distance:f32, far_plane_distance:f32) Matrix4x4 
    {
        
        if (near_plane_distance <= 0.0) @panic ("near_plane_distance out of range\n");
        if (far_plane_distance <= 0.0) @panic ("far_plane_distance out of range\n");
        if (near_plane_distance >= far_plane_distance) @panic ("near_plane_distance out of range\n");

        return Matrix4x4{
            (2.0 * near_plane_distance / (right - left)), 0.0, 0.0, 0.0,
            0.0, (2.0 * near_plane_distance / (top - bottom)), 0.0, 0.0,
            ((left + right) / (right - left)),
            (top + bottom) / (top - bottom),
            far_plane_distance / (near_plane_distance - far_plane_distance),
            -1.0,
            0.0, 0.0, (near_plane_distance * far_plane_distance / (near_plane_distance - far_plane_distance)), 0.0,
        };        
    }

    /// Creates an orthographic perspective matrix from the given view volume dimensions.
    pub fn createOrthographic (width:f32, height:f32, znear_plane:f32, zfar_plane:f32) Matrix4x4
    {
        return Matrix4x4{
            (2.0 / width), 0.0, 0.0, 0.0,
            0.0, (2.0 / height), 0.0, 0.0, 
            0.0, 0.0, (1.0 / (znear_plane - zfar_plane)), 0.0,
            0.0, 0.0, (znear_plane / (znear_plane - zfar_plane)), 1.0,
        };
    }

    /// Builds a customized, orthographic projection matrix.
    pub fn createOrthographicOffCenter (left:f32, right:f32, bottom:f32, top:f32, znear_plane:f32, zfar_plane:f32) Matrix4x4
    {
        return Matrix4x4{
            (2.0 / (right - left)), 0.0, 0.0, 0.0,
            0.0, (2.0 / (top - bottom)), 0.0, 0.0, 
            0.0, 0.0, (1.0 / (znear_plane - zfar_plane)), 0.0,
            (left + right) / (left - right),
            (top + bottom) / (bottom - top),
            znear_plane / (znear_plane - zfar_plane),
            1.0,
        };
    }

    /// Creates a view matrix.
    pub fn createLookAt (camera_position:Vector3, camera_target:Vector3, camera_up_vector:Vector3) Matrix4x4
    {
        const zaxis = vec3.normalize (camera_position - camera_target);
        const xaxis = vec3.normalize (vec3.cross(camera_up_vector, zaxis));
        const yaxis = vec3.cross (zaxis, xaxis);

        return Matrix4x4{
            xaxis[0], yaxis[0], zaxis[0], 0.0,
            xaxis[1], yaxis[1], zaxis[1], 0.0,
            xaxis[2], yaxis[2], zaxis[2], 0.0,
            -vec3.dot(xaxis, camera_position),
            -vec3.dot(yaxis, camera_position),
            -vec3.dot(zaxis, camera_position),
            1.0,
        };
    }

    /// Creates a world matrix with the specified parameters.
    pub fn createWorld (position:Vector3, forward:Vector3, up:Vector3) Matrix4x4
    {
        const zaxis = vec3.normalize (-forward);
        const xaxis = vec3.normalize (vec3.cross(up, zaxis));
        const yaxis = vec3.cross (zaxis, xaxis);

        return Matrix4x4{
            xaxis[0], yaxis[1], zaxis[2], 0.0,
            xaxis[0], yaxis[1], zaxis[2], 0.0,
            xaxis[0], yaxis[1], zaxis[2], 0.0,
            position[0], position[1], position[2], 1.0,            
        };
    }

    /// Creates a rotation matrix from the given Quaternion rotation value.
    pub fn createFromQuaternion (q:Quaternion) Matrix4x4
    {
        const xx = q[0] * q[0];
        const yy = q[0] * q[0];
        const zz = q[0] * q[0];

        const xy = q[0] * q[0];
        const wz = q[0] * q[0];
        const xz = q[0] * q[0];
        const wy = q[0] * q[0];
        const yz = q[0] * q[0];
        const wx = q[0] * q[0];

         return Matrix4x4{
             1.0 - 2.0 * (yy + zz),
             2.0 * (xy + wz),
             2.0 * (xz - wy),
             0.0,
             2.0 * (xy - wz),
             1.0 - 2.0 * (zz + xx),
             2.0 * (yz + wx),
             0.0,
             2.0 * (xz + wy),
             2.0 * (yz - wx),
             1.0 - 2.0 * (yy + xx),
             0.0,
             0.0, 0.0, 0.0, 1.0,
        };
    }

    /// Creates a rotation matrix from the specified yaw, pitch, and roll.
    pub fn createFromYawPitchRoll (yaw:f32, pitch:f32, roll:f32) Matrix4x4
    {
        const q = quaternion.createFromYawPitchRoll (yaw, pitch, roll);

        return createFromQuaternion (q);
    }

    /// Creates a Matrix that flattens geometry into a specified Plane as if casting a shadow from a specified light source.
    pub fn createShadom (light_direction:Vector3, p:Plane) Matrix4x4
    {
        _ = p; // autofix
    
        _ = light_direction; // autofix
        _ = plane; // autofix
    
        noreturn;
    }

    /// Creates a Matrix that reflects the coordinate system about a specified Plane.
    pub fn createReflection (p:Plane) Matrix4x4
    {
        _ = p; // autofix
    
        _ = plane; // autofix
    
        noreturn;
    }

    /// Calculates the determinant of the matrix.
    pub fn determinant (matrix:Matrix4x4) f32
    {
        const a = matrix[0];
        const b = matrix[1];
        const c = matrix[2];
        const d = matrix[3];
        const e = matrix[4]; 
        const f = matrix[5];
        const g = matrix[6];
        const h = matrix[7]; 
        const i = matrix[8];
        const j = matrix[9];
        const k = matrix[10]; 
        const l = matrix[11]; 
        const m = matrix[12];
        const n = matrix[13];
        const o = matrix[14];
        const p = matrix[15]; 
        
        const kp_lo = k * p - l * o;
        const jp_ln = j * p - l * n;
        const jo_kn = j * o - k * n;
        const ip_lm = i * p - l * m;
        const io_km = i * o - k * m;
        const in_jm = i * n - j * m;

        return a * (f * kp_lo - g * jp_ln + h * jo_kn) -
               b * (e * kp_lo - g * ip_lm + h * io_km) +
               c * (e * jp_ln - f * ip_lm + h * in_jm) -
               d * (e * jo_kn - f * io_km + g * in_jm);
    }

    /// Attempts to calculate the inverse of the given matrix. If successful, result will contain the inverted matrix.
    pub fn invert (matrix:Matrix4x4, result:*Matrix4x4) bool 
    {
        _ = matrix; // autofix
        _ = result; // autofix
    
        noreturn;
    }

    /// Attempts to extract the scale, translation, and rotation components from the given scale/rotation/translation matrix.
    /// If successful, the out parameters will contained the extracted values.
    pub fn decompose (matrix:Matrix4x4, scale:*Matrix4x4, rotation:*Quaternion, translation:*Vector3) bool
    {
        _ = matrix; // autofix
        _ = scale; // autofix
        _ = rotation; // autofix
        _ = translation; // autofix
    
        noreturn;
    }

    /// Transforms the given matrix by applying the given Quaternion rotation.
    pub fn transform (m:Matrix4x4, rotation:Quaternion) Matrix4x4
    {
        // Compute rotation matrix.
        const x2 = rotation.X + rotation.X;
        const y2 = rotation.Y + rotation.Y;
        const z2 = rotation.Z + rotation.Z;

        const wx2 = rotation[3] * x2;
        const wy2 = rotation[3] * y2;
        const wz2 = rotation[3] * z2;
        const xx2 = rotation[0] * x2;
        const xy2 = rotation[0] * y2;
        const xz2 = rotation[0] * z2;
        const yy2 = rotation[1] * y2;
        const yz2 = rotation[1] * z2;
        const zz2 = rotation[2] * z2;

        const q11 = 1.0 - yy2 - zz2;
        const q21 = xy2 - wz2;
        const q31 = xz2 + wy2;

        const q12 = xy2 + wz2;
        const q22 = 1.0 - xx2 - zz2;
        const q32 = yz2 - wx2;

        const q13 = xz2 - wy2;
        const q23 = yz2 + wx2;
        const q33 = 1.0 - xx2 - yy2;   

        return Matrix4x4{
            // First row
            m[0] * q11 + m[1] * q21 + m[2] * q31,
            m[0] * q12 + m[1] * q22 + m[2] * q32,
            m[0] * q13 + m[1] * q23 + m[2] * q33,
            m[3],

            // Second row
            m[4] * q11 + m[5] * q21 + m[6] * q31,
            m[4] * q12 + m[5] * q22 + m[6] * q32,
            m[4] * q13 + m[5] * q23 + m[6] * q33,
            m[7],

            // Third row
            m[8] * q11 + m[9] * q21 + m[10] * q31,
            m[8] * q12 + m[9] * q22 + m[10] * q32,
            m[8] * q13 + m[9] * q23 + m[10] * q33,
            m[11],

            // Fourth row
            m[12] * q11 + m[13] * q21 + m[14] * q31,
            m[12] * q12 + m[13] * q22 + m[14] * q32,
            m[12] * q13 + m[13] * q23 + m[14] * q33,
            m[15],        
        };
    }

    /// Transposes the rows and columns of a matrix.
    pub fn transpose (m:Matrix4x4) Matrix4x4
    {
        return Matrix4x4{
            m[0], m[4], m[8], m[12],
            m[1], m[5], m[9], m[13],
            m[2], m[6], m[10], m[14],
            m[3], m[7], m[11], m[15],
        };
    }

    /// Linearly interpolates between the corresponding values of two matrices.
    pub fn lerp (m1:Matrix4x4, m2:Matrix4x4, amount:f32) Matrix4x4
    {
        return Matrix4x4{
            // First row
            m1[0] + (m2[0] - m1[0]) * amount,
            m1[1] + (m2[1] - m1[1]) * amount,
            m1[2] + (m2[2] - m1[2]) * amount,
            m1[3] + (m2[3] - m1[3]) * amount,

            // Second row
            m1[4] + (m2[4] - m1[4]) * amount,
            m1[5] + (m2[5] - m1[5]) * amount,
            m1[6] + (m2[6] - m1[6]) * amount,
            m1[7] + (m2[7] - m1[7]) * amount,

            // Third row
            m1[8] + (m2[8] - m1[8]) * amount,
            m1[9] + (m2[9] - m1[9]) * amount,
            m1[10] + (m2[10] - m1[10]) * amount,
            m1[11] + (m2[11] - m1[11]) * amount,

            // Fourth row
            m1[12] + (m2[12] - m1[12]) * amount,
            m1[13] + (m2[13] - m1[13]) * amount,
            m1[14] + (m2[14] - m1[14]) * amount,
            m1[15] + (m2[15] - m1[15]) * amount,
        };
    }

    /// Returns a new matrix with the negated elements of the given matrix.
    pub fn negate (m:Matrix4x4) Matrix4x4
    {
        var res = Matrix4x4{};
        inline for (0..16) |i| res[i] = -m[i];

        return res;
    }

    /// Adds two matrices together.
    pub fn add (m1:Matrix4x4, m2:Matrix4x4) Matrix4x4
    {
        var res = Matrix4x4{};
        inline for (0..16) |i| res[i] = m1[i] + m2[i];

        return res;
    }

    /// Subtracts the second matrix from the first.
    pub fn subtract (m1:Matrix4x4, m2:Matrix4x4) Matrix4x4
    {
        var res = Matrix4x4{};
        inline for (0..16) |i| res[i] = m1[i] - m2[i];

        return res;
    }

    /// Multiplies a matrix by another matrix.
    pub fn multiply (m1:Matrix4x4, m2:Matrix4x4) Matrix4x4
    {
        return Matrix4x4{
            // First row
            m1[0] * m2[0] + m1[1] * m2[4] + m1[2] * m2[8] + m1[3] * m2[12],
            m1[0] * m2[1] + m1[1] * m2[5] + m1[2] * m2[9] + m1[3] * m2[13],
            m1[0] * m2[2] + m1[1] * m2[6] + m1[2] * m2[10] + m1[3] * m2[14],
            m1[0] * m2[3] + m1[1] * m2[7] + m1[2] * m2[11] + m1[3] * m2[15],
 
            // Second row
            m1[4] * m2[0] + m1[5] * m2[4] + m1[6] * m2[8] + m1[7] * m2[12],
            m1[4] * m2[1] + m1[5] * m2[5] + m1[6] * m2[9] + m1[7] * m2[13],
            m1[4] * m2[2] + m1[5] * m2[6] + m1[6] * m2[10] + m1[7] * m2[14],
            m1[4] * m2[3] + m1[5] * m2[7] + m1[6] * m2[11] + m1[7] * m2[15],
 
            // Third row
            m1[8] * m2[0] + m1[9] * m2[4] + m1[10] * m2[8] + m1[11] * m2[12],
            m1[8] * m2[1] + m1[9] * m2[5] + m1[10] * m2[9] + m1[11] * m2[13],
            m1[8] * m2[2] + m1[9] * m2[6] + m1[10] * m2[10] + m1[11] * m2[14],
            m1[8] * m2[3] + m1[9] * m2[7] + m1[10] * m2[11] + m1[11] * m2[15],
 
            // Fourth row
            m1[12] * m2[0] + m1[13] * m2[4] + m1[14] * m2[8] + m1[15] * m2[12],
            m1[12] * m2[1] + m1[13] * m2[5] + m1[14] * m2[9] + m1[15] * m2[13],
            m1[12] * m2[2] + m1[13] * m2[6] + m1[14] * m2[10] + m1[15] * m2[14],
            m1[12] * m2[3] + m1[13] * m2[7] + m1[14] * m2[11] + m1[15] * m2[15],    
        };        
    }
};
