#version 410 core

out vec4 FragColor;

in vec3 FragPos;
in vec4 Color;

void main()
{
    vec2 temp = gl_PointCoord - vec2(0.5);
    float f = dot(temp, temp);
    if (f>0.25) discard;
    FragColor = Color;        
}
