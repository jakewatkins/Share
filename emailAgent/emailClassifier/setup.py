#!/usr/bin/env python3
"""
Email Classifier Setup Script
Installs required dependencies for the Email Classifier
"""

import subprocess
import sys
import os
import venv
from pathlib import Path

def create_virtual_environment():
    """Create a virtual environment for the project"""
    venv_path = Path("venv")

    if venv_path.exists():
        print("✅ Virtual environment already exists")
        return venv_path

    try:
        print("🔧 Creating virtual environment...")
        venv.create(venv_path, with_pip=True)
        print("✅ Virtual environment created successfully")
        return venv_path
    except Exception as e:
        print(f"❌ Failed to create virtual environment: {e}")
        return None

def get_venv_pip(venv_path):
    """Get the pip executable from virtual environment"""
    if sys.platform == "win32":
        return venv_path / "Scripts" / "pip"
    else:
        return venv_path / "bin" / "pip"

def get_venv_python(venv_path):
    """Get the python executable from virtual environment"""
    if sys.platform == "win32":
        return venv_path / "Scripts" / "python"
    else:
        return venv_path / "bin" / "python"

def install_package_with_fallback(package, venv_path=None):
    """Install a Python package with fallback options"""
    try:
        if venv_path:
            # Try virtual environment first
            pip_cmd = get_venv_pip(venv_path)
            print(f"Installing {package} in virtual environment...")
            subprocess.check_call([str(pip_cmd), "install", package])
        else:
            # Try regular pip install
            print(f"Installing {package}...")
            subprocess.check_call([sys.executable, "-m", "pip", "install", package])

        print(f"✅ {package} installed successfully")
        return True

    except subprocess.CalledProcessError as e:
        # If regular install fails, try with --user flag
        if not venv_path:
            try:
                print(f"Retrying {package} with --user flag...")
                subprocess.check_call([sys.executable, "-m", "pip", "install", "--user", package])
                print(f"✅ {package} installed successfully (user install)")
                return True
            except subprocess.CalledProcessError:
                pass

        print(f"❌ Failed to install {package}: {e}")
        return False

def install_package(package):
    """Install a Python package using pip"""
    try:
        print(f"Installing {package}...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", package])
        print(f"✅ {package} installed successfully")
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to install {package}: {e}")
        return False
    return True

def check_mlx_availability():
    """Check if we're on Apple Silicon Mac for MLX support"""
    try:
        import platform
        if platform.system() != "Darwin":
            print("❌ MLX framework requires macOS (Apple Silicon)")
            return False

        # Try to detect Apple Silicon
        machine = platform.machine()
        if machine not in ["arm64"]:
            print("⚠️  MLX framework is optimized for Apple Silicon Macs")
            print(f"   Detected architecture: {machine}")

        return True
    except Exception:
        return False

def main():
    """Main setup function"""
    print("🚀 Setting up Email Classifier Dependencies")
    print("=" * 50)

    # Check system compatibility
    if not check_mlx_availability():
        print("\n⚠️  System compatibility warning noted, continuing with installation...")

    # List of required packages
    required_packages = [
        "mlx-lm",      # MLX Language Models
        "mlx",         # MLX Core
        "transformers", # Hugging Face Transformers (may be needed)
    ]

    # Ask user about installation method
    print("\n📦 Installation Options:")
    print("1. Create virtual environment (recommended)")
    print("2. Install to user directory (--user)")
    print("3. Try system-wide install")
    print("4. Show manual install instructions")

    # Use virtual environment by default when run non-interactively
    import sys
    if hasattr(sys.stdin, 'isatty') and not sys.stdin.isatty():
        choice = "1"
        print("Running non-interactively, using virtual environment...")
    else:
        choice = input("\nChoose option (1-4) [default: 1]: ").strip() or "1"

    venv_path = None
    if choice == "1":
        venv_path = create_virtual_environment()
        if not venv_path:
            print("Virtual environment creation failed, falling back to user install...")
            choice = "2"

    # Install packages
    failed_packages = []

    if choice == "1" and venv_path:
        for package in required_packages:
            if not install_package_with_fallback(package, venv_path):
                failed_packages.append(package)
    elif choice == "2":
        for package in required_packages:
            try:
                print(f"Installing {package} with --user flag...")
                subprocess.check_call([sys.executable, "-m", "pip", "install", "--user", package])
                print(f"✅ {package} installed successfully")
            except subprocess.CalledProcessError as e:
                print(f"❌ Failed to install {package}: {e}")
                failed_packages.append(package)
    elif choice == "3":
        for package in required_packages:
            if not install_package(package):
                failed_packages.append(package)
    else:  # choice == "4"
        print("\n📋 Manual Installation Instructions:")
        print("=" * 40)
        print("Option 1 - Virtual Environment (Recommended):")
        print("  python3 -m venv venv")
        print("  source venv/bin/activate  # On Windows: venv\\Scripts\\activate")
        for package in required_packages:
            print(f"  pip install {package}")
        print("\nOption 2 - User Install:")
        for package in required_packages:
            print(f"  pip install --user {package}")
        print("\nOption 3 - Homebrew Python:")
        print("  brew install python@3.11")
        for package in required_packages:
            print(f"  pip3 install {package}")
        return

    print("\n" + "=" * 50)

    if failed_packages:
        print("❌ Setup completed with errors!")
        print(f"Failed to install: {', '.join(failed_packages)}")
        print("\n🔧 Alternative installation methods:")
        print("1. Try virtual environment:")
        print("   python3 -m venv venv")
        print("   source venv/bin/activate")
        for package in failed_packages:
            print(f"   pip install {package}")
        print("\n2. Try user installation:")
        for package in failed_packages:
            print(f"   pip install --user {package}")
    else:
        print("✅ All dependencies installed successfully!")

        if venv_path:
            print("\n🎯 To use the Email Classifier:")
            print("1. Activate the virtual environment:")
            print("   source venv/bin/activate  # On Windows: venv\\Scripts\\activate")
            print("2. Configure settings.json")
            print("3. Run: python emailClassifier.py")
            print("4. When done: deactivate")
        else:
            print("\nEmail Classifier is ready to use!")
            print("\nNext steps:")
            print("1. Configure settings.json with your paths and model")
            print("2. Run: python emailClassifier.py")

    print("\n📖 For MLX installation issues, see:")
    print("   https://github.com/ml-explore/mlx")

if __name__ == "__main__":
    main()
