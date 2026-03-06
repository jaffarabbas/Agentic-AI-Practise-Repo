import torch
import torch.nn as nn
import torch.optim as optim
from torchvision import datasets, transforms
from torch.utils.data import DataLoader
import time

# ===============================
# Device Setup (AMD / NVIDIA / CPU)
# ===============================

try:
    import torch_directml
    device = torch_directml.device()
    print("Running on AMD GPU (DirectML)")
except ImportError:
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Running on: {device}")

# Test tensor location
test_tensor = torch.randn(1).to(device)
print("Tensor device:", test_tensor.device)
print("-" * 50)

# ===============================
# Model Definition
# ===============================

class SimpleCNN(nn.Module):
    def __init__(self):
        super(SimpleCNN, self).__init__()
        self.conv1 = nn.Conv2d(1, 32, 3, 1)
        self.conv2 = nn.Conv2d(32, 64, 3, 1)
        self.dropout1 = nn.Dropout(0.25)
        self.dropout2 = nn.Dropout(0.5)
        self.fc1 = nn.Linear(9216, 128)
        self.fc2 = nn.Linear(128, 10)

    def forward(self, x):
        x = torch.relu(self.conv1(x))
        x = torch.relu(self.conv2(x))
        x = torch.max_pool2d(x, 2)
        x = self.dropout1(x)
        x = torch.flatten(x, 1)
        x = torch.relu(self.fc1(x))
        x = self.dropout2(x)
        x = self.fc2(x)
        return x

# ===============================
# Dataset
# ===============================

transform = transforms.Compose([
    transforms.ToTensor(),
    transforms.Normalize((0.1307,), (0.3081,))
])

print("Downloading MNIST dataset...")
train_dataset = datasets.MNIST('./data', train=True, download=True, transform=transform)

# Slightly larger batch size for better GPU usage
train_loader = DataLoader(train_dataset, batch_size=128, shuffle=True)

# ===============================
# Initialize Model
# ===============================

model = SimpleCNN().to(device)
criterion = nn.CrossEntropyLoss()

# IMPORTANT: Disable foreach for DirectML compatibility
optimizer = optim.Adam(
    model.parameters(),
    lr=0.001,
    foreach=False
)

# ===============================
# Training Loop
# ===============================

print("\nStarting training...\n")
model.train()
start_time = time.time()

for epoch in range(3):
    epoch_loss = 0
    correct = 0
    total = 0

    for batch_idx, (data, target) in enumerate(train_loader):
        data = data.to(device)
        target = target.to(device)

        optimizer.zero_grad()
        output = model(data)
        loss = criterion(output, target)
        loss.backward()
        optimizer.step()

        epoch_loss += loss.item()

        _, predicted = torch.max(output, 1)
        total += target.size(0)
        correct += (predicted == target).sum().item()

        if batch_idx % 100 == 0:
            print(
                f'Epoch {epoch+1}/3 | '
                f'Batch {batch_idx}/{len(train_loader)} | '
                f'Loss: {loss.item():.4f}'
            )

    avg_loss = epoch_loss / len(train_loader)
    accuracy = 100 * correct / total

    print(f"\nEpoch {epoch+1} Completed")
    print(f"Average Loss: {avg_loss:.4f}")
    print(f"Accuracy: {accuracy:.2f}%")
    print("-" * 50)

end_time = time.time()

print("\nTraining completed!")
print(f"Total time: {end_time - start_time:.2f} seconds")

print("\nIf GPU usage increased in Task Manager → you are training on AMD GPU 🚀")
