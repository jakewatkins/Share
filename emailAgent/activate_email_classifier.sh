#!/bin/bash
# Email Classifier Virtual Environment Activation Script

echo "🚀 Activating Email Classifier Environment"
echo "=========================================="

# Check if virtual environment exists
if [ ! -d "venv" ]; then
    echo "❌ Virtual environment not found!"
    echo "   Run: python3 emailClassifier/setup.py"
    exit 1
fi

# Activate virtual environment
source venv/bin/activate

echo "✅ Virtual environment activated"
echo ""
echo "📋 Available commands:"
echo "   python emailClassifier/emailClassifier.py  - Run email classifier"
echo "   deactivate                                  - Exit virtual environment"
echo ""
echo "📖 Next steps:"
echo "   1. Configure emailClassifier/settings.json"
echo "   2. Run the email classifier"
echo ""

# Keep shell active in virtual environment
exec "$SHELL"
