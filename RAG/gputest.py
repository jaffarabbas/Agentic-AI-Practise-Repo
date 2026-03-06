import torch

# Try DirectML first (AMD)
try:
    import torch_directml
    device = torch_directml.device()
    print("Running on AMD GPU (DirectML)")
except ImportError:
    # Fallback to CUDA (NVIDIA) or CPU
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Running on: {device}")

# Test tensor location
x = torch.randn(1).to(device)
print("Tensor device:", x.device)
