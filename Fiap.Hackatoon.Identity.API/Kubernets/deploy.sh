set -e  # Encerra o script em caso de erro

echo "🚪 Aplicando Rabbitmq Deployment.."
kubectl apply -f RabbitmqDeployment.yaml  # Aplica o Service da aplicação

echo "🚪 Aplicando Rabbitmq Service..."
kubectl apply -f RabbitmqService.yaml  # Aplica o Service da aplicação

echo "🚪 Aplicando SQl Deployment.."
kubectl apply -f SqlDeployment.yaml  # Aplica o Service da aplicação

echo "🚪 Aplicando Sql Service..."
kubectl apply -f SQlServices.yaml  # Aplica o Service da aplicação

echo "🔐 Aplicando Secret da aplicação..."
kubectl apply -f app-secrets.yaml  # Adiciona o Secret

echo "⚙️ Aplicando ConfigMap da aplicação..."
kubectl apply -f Configmap.yaml  # Aplica o ConfigMap

echo "📦 Aplicando Deployment da aplicação..."
kubectl apply -f Deployment.yaml  # Aplica o Deployment da aplicação

echo "🚪 Aplicando Service da aplicação..."
kubectl apply -f Service.yaml  # Aplica o Service da aplicação

echo "🚪 Aplicando Service da aplicação..."
kubectl apply -f hpa.yaml  # Aplica o Service da aplicação

echo "✅ Tudo aplicado com sucesso!"