#version 410 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 FragPos;
out vec4 Color;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    // gl_PointSize = (1.0 - aPos.y / aPos.z) * 2.0;
    gl_PointSize = 4.0;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    Color = aColor;
}
