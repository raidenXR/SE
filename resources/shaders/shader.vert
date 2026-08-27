#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec4 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 Normal;
out vec3 FragPos;
out vec4 Color;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    Normal = aNormal * mat3(transpose(inverse(model)));
    Color = aColor;
}
