# Email Classifier

A Python script that uses Apple's MLX framework to classify emails using a fine-tuned Large Language Model (LLM).

## 🎯 Purpose

This script processes JSON email files and automatically classifies them into predefined categories using a fine-tuned email classification model. It's designed for unattended operation with comprehensive logging and robust error handling.

## 🔧 Requirements

- **macOS** (Apple Silicon recommended for optimal MLX performance)
- **Python 3.8+**
- **Apple MLX Framework**

## 📦 Installation

1. **Clone/Download the script**:
   ```bash
   cd emailAgent/emailClassifier
   ```

2. **Install dependencies**:
   ```bash
   python setup.py
   ```

   Or manually install:
   ```bash
   pip install mlx-lm mlx transformers
   ```

## ⚙️ Configuration

Edit `settings.json` to configure the classifier:

```json
{
    "Classifier": "jake-watkins/email-classifier",
    "EmailTemp": "/Users/jakewatkins/temp/email",
    "ProcessedEmails": "/Users/jakewatkins/temp/processedEmails",
    "Prompt": "Classify the following email..."
}
```

### Configuration Options:
- **Classifier**: HuggingFace model identifier for the fine-tuned email classifier
- **EmailTemp**: Directory containing JSON email files to process
- **ProcessedEmails**: Directory where classified emails will be saved
- **Prompt**: Classification prompt template (includes placeholder `{email_body}`)

## 🚀 Usage

```bash
python emailClassifier.py
```

## 📊 Classification Categories

The model classifies emails into these categories:
- **promotional** - Marketing and sales emails
- **transactional** - Receipts, confirmations, account updates
- **notification** - System notifications, alerts
- **security** - Security alerts, password resets
- **event** - Event invitations, calendar items
- **educational** - Learning content, tutorials
- **newsletter** - Regular newsletters, digests
- **survey** - Surveys, feedback requests
- **business** - Business communications
- **personal** - Personal correspondence
- **solicitation** - Requests for donations, sales pitches
- **recruitment** - Job offers, recruitment emails
- **membership** - Membership-related communications
- **political** - Political campaigns, advocacy
- **informative** - News, informational content
- **account** - Account management, billing
- **press** - Press releases, media communications
- **memorial** - Memorial notices, obituaries
- **admission** - School/program admissions

## 📄 Output

The script creates a timestamped output file:
```
classified-emails-20260201143022.json
```

Each email gets a `classification` field added:
```json
{
  "id": "123456789",
  "service": "Gmail",
  "from": "sender@example.com",
  "subject": "Special Offer!",
  "body": "...",
  "classification": "promotional"
}
```

## 📋 Process Flow

1. **Load Configuration** from `settings.json`
2. **Validate Directories** (EmailTemp and ProcessedEmails must exist)
3. **Load MLX Model** from HuggingFace
4. **Process Email Files**:
   - Find all `.json` files in EmailTemp directory
   - For each email in each file:
     - Show progress with blue dots (`.`)
     - Classify using the LLM
     - Add classification field to email
     - Append to output file
   - Show completion with green exclamation (`!`)
   - Rename processed file to `.bak`
5. **Complete** with green "done" message

## 🔍 Progress Indicators

- **Blue dots (`.`)**: Processing individual emails
- **Green exclamation (`!`)**: Completed processing a file
- **Red hash (`#`)**: Classification error (falls back to "other")
- **Green "done"**: All files processed successfully

## 📝 Logging

Detailed logs are saved to timestamped log files:
```
emailClassifier-20260201143022.log
```

Logs include:
- Script start/completion times
- Model loading progress
- File processing status
- Errors and warnings
- Summary statistics

## 🛠️ Error Handling

- **Missing directories**: Script exits with error message
- **Model loading failure**: Script exits with "LLM not available"
- **Invalid JSON files**: Script exits with specific error
- **Invalid classifications**: Shows red `#`, uses "other", continues
- **Empty email bodies**: Skipped (logged)

## 💡 Performance Notes

- **Model Loading**: Loads once at startup for all emails
- **Memory Usage**: Utilizes all available MLX resources
- **File Processing**: Processes files sequentially
- **Output Format**: Maintains valid JSON array format

## 🔧 Troubleshooting

### MLX Installation Issues
```bash
# Make sure you're on Apple Silicon Mac
arch
# Should show: arm64

# Install MLX directly
pip install mlx mlx-lm
```

### Model Download Issues
- Ensure internet connection for HuggingFace model download
- Check model identifier in configuration
- Verify HuggingFace credentials if using private models

### Directory Permission Issues
- Ensure read access to EmailTemp directory
- Ensure write access to ProcessedEmails directory
- Check file permissions for JSON email files

## 📊 Example Run

```bash
$ python emailClassifier.py
Loading model jake-watkins/email-classifier...
Model loaded successfully!
....................!..............!.........!
done
```

Check the log file for detailed statistics and any issues encountered during processing.

## 🎯 Integration

This classifier is designed to work with the Email Agent system:
1. **Email Agent** retrieves emails → saves to JSON files
2. **Email Classifier** processes JSON files → adds classifications
3. **Analysis Tools** can use classified emails for insights

Perfect for building email analytics pipelines! 🚀
