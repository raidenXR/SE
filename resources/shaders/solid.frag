#version 410 core

out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;
in vec4 Color;

void main()
{
    FragColor = Color;        
}
