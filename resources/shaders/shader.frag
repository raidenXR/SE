#version 330 core

struct Material {
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;    
};

struct DirLight {
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct PointLight {
    vec3 position;
    float constant;
    float linear;
    float quadratic;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;    
};

struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float constant;
    float linear;
    float quadratic;
}; 

uniform vec3 viewPos;
uniform Material material;
uniform DirLight dirLight;
uniform PointLight pointLight;
uniform SpotLight spotLight;

out vec4 FragColor;

in vec3 Normal;
in vec3 FragPos;
in vec4 Color;

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

void main()
{
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);

    vec3 result = CalcDirLight(dirLight, norm, viewDir);
    result += CalcPointLight(pointLight, norm, FragPos, viewDir);
    result += CalcSpotLight(spotLight, norm, FragPos, viewDir);
    
    FragColor = vec4(result, 1.0);        
}

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    vec3 ambient = light.ambient * (material.diffuse * vec3(Color));
    vec3 diffuse = light.diffuse * (diff * material.diffuse * vec3(Color));
    vec3 specular = light.specular * (diff * material.specular * vec3(Color));

    return (ambient + diffuse + specular);
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - FragPos);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

    vec3 ambient = light.ambient * (material.diffuse * vec3(Color));
    vec3 diffuse = light.diffuse * (diff * material.diffuse * vec3(Color));
    vec3 specular = light.specular * (spec * material.specular * vec3(Color));
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;

    return (ambient * diffuse * specular);        
}

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - FragPos);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    float distance = length(light.position - FragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));        

    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff ) / epsilon, 0.0, 1.0);

    vec3 ambient = light.ambient * (material.diffuse * vec3(Color));
    vec3 diffuse = light.diffuse * (diff * material.diffuse * vec3(Color));
    vec3 specular = light.specular * (spec * material.specular * vec3(Color));
    ambient *= attenuation;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;

    return (ambient + diffuse + specular);
}
    
