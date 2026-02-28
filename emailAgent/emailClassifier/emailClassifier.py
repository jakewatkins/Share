#!/usr/bin/env python3
"""
Email Classifier - Uses Apple MLX framework to classify emails using a fine-tuned LLM
"""

import json
import os
import sys
import glob
import logging
import re
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Any, Optional

try:
    import mlx.core as mx
    import mlx.nn as nn
    from mlx_lm import load, generate
except ImportError:
    print("Error: MLX framework not found. Please install mlx-lm package.")
    sys.exit(1)

class EmailClassifier:
    def __init__(self, config_path: str = "settings.json"):
        self.config = self._load_configuration(config_path)
        self.model = None
        self.tokenizer = None
        self.logger = self._setup_logging()
        self.stats = {
            'total_emails': 0,
            'classifications_made': 0,
            'errors': 0,
            'files_processed': 0
        }

    def _load_configuration(self, config_path: str) -> Dict[str, Any]:
        """Load configuration from JSON file"""
        try:
            if not os.path.exists(config_path):
                print(f"Error: Configuration file '{config_path}' not found.")
                sys.exit(1)

            with open(config_path, 'r') as f:
                config = json.load(f)

            # Validate required configuration keys
            required_keys = ['Classifier', 'EmailTemp', 'ProcessedEmails', 'Prompt']
            for key in required_keys:
                if key not in config:
                    print(f"Error: Missing required configuration key: {key}")
                    sys.exit(1)

            return config
        except json.JSONDecodeError as e:
            print(f"Error: Invalid JSON in configuration file: {e}")
            sys.exit(1)
        except Exception as e:
            print(f"Error loading configuration: {e}")
            sys.exit(1)

    def _setup_logging(self) -> logging.Logger:
        """Setup logging with timestamped log file"""
        timestamp = datetime.now().strftime("%Y%m%d%H%M%S")
        log_filename = f"emailClassifier-{timestamp}.log"

        # Create logger
        logger = logging.getLogger('EmailClassifier')
        logger.setLevel(logging.INFO)

        # Create file handler
        file_handler = logging.FileHandler(log_filename)
        file_handler.setLevel(logging.INFO)

        # Create formatter
        formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s',
                                    datefmt='%Y-%m-%d %H:%M:%S')
        file_handler.setFormatter(formatter)

        # Add handler to logger
        logger.addHandler(file_handler)

        return logger

    def _validate_directories(self) -> None:
        """Validate that required directories exist"""
        if not os.path.exists(self.config['EmailTemp']):
            print(f"Error: EmailTemp directory '{self.config['EmailTemp']}' does not exist.")
            self.logger.error(f"EmailTemp directory '{self.config['EmailTemp']}' does not exist.")
            sys.exit(1)

        if not os.path.exists(self.config['ProcessedEmails']):
            print(f"Error: ProcessedEmails directory '{self.config['ProcessedEmails']}' does not exist.")
            self.logger.error(f"ProcessedEmails directory '{self.config['ProcessedEmails']}' does not exist.")
            sys.exit(1)

    def load_model(self) -> None:
        """Load the MLX model from HuggingFace"""
        try:
            self.logger.info(f"Starting model loading: {self.config['Classifier']}")
            print(f"Loading model {self.config['Classifier']}...")

            # Load model and tokenizer using MLX
            self.model, self.tokenizer = load(self.config['Classifier'])

            self.logger.info("Model loading completed successfully")
            print("Model loaded successfully!")

        except Exception as e:
            error_msg = f"Error loading model: {e}"
            print(f"Error: {error_msg}")
            self.logger.error(error_msg)
            print("Error: LLM is not available")
            sys.exit(1)

    def get_email_files(self) -> List[str]:
        """Get list of JSON email files to process"""
        email_temp_path = Path(self.config['EmailTemp'])
        json_files = list(email_temp_path.glob("*.json"))

        if not json_files:
            self.logger.warning("No JSON files found in EmailTemp directory")
            print("No email files to process.")
            return []

        return [str(f) for f in json_files]

    def _strip_html(self, html_content: str) -> str:
        """Strip HTML tags and decode HTML entities from email content"""
        if not html_content:
            return ""

        # Remove HTML tags
        clean = re.sub(r'<[^>]+>', '', html_content)

        # Replace common HTML entities
        clean = clean.replace('&nbsp;', ' ')
        clean = clean.replace('&amp;', '&')
        clean = clean.replace('&lt;', '<')
        clean = clean.replace('&gt;', '>')
        clean = clean.replace('&quot;', '"')
        clean = clean.replace('&#39;', "'")

        # Clean up whitespace
        clean = re.sub(r'\s+', ' ', clean)
        clean = clean.strip()

        return clean

    def classify_email(self, email_body: str) -> str:
        """Classify a single email using the LLM"""
        try:
            debug = self.config.get('Debug', False)

            # Strip HTML from email body
            clean_email_body = self._strip_html(email_body)

            if debug:
                print(f"Debug: Original length: {len(email_body)}, Clean length: {len(clean_email_body)}")

            # Prepare the prompt with cleaned email body
            prompt = self.config['Prompt'].replace('{email_body}', clean_email_body)

            # Generate classification using MLX - simplified parameters
            response = generate(
                self.model,
                self.tokenizer,
                prompt,
                max_tokens=10
            )

            # Extract the classification (should be a single word)
            classification = response.strip().lower()

            if debug:
                print(f"Debug: Classification result: '{classification}'")
            else:
                print('.', end='', flush=True)  # Show progress dot

            # Validate classification against known categories
            valid_categories = [
                'promotional', 'transactional', 'notification', 'security', 'event',
                'educational', 'newsletter', 'survey', 'business', 'personal',
                'solicitation', 'recruitment', 'membership', 'political', 'informative',
                'account', 'press', 'memorial', 'admission'
            ]

            # if classification in valid_categories:
            #     return classification
            # else:
            #     # Invalid classification - print red # and return "other"
            #     print('#', end='', flush=True)
            #     self.stats['errors'] += 1
            #     self.logger.warning(f"Invalid classification returned: {classification}, using 'other'")
            #     return 'other'

            return classification

        except Exception as e:
            # Error during classification - print red # and return "other"
            print(f"error: {e}", end='', flush=True)
            self.stats['errors'] += 1
            self.logger.error(f"Error during classification: {e}")
            return 'other'

    def process_email_file(self, file_path: str, output_file: str) -> None:
        """Process a single email file"""
        try:
            self.logger.info(f"Starting processing of file: {file_path}")

            # Load email file
            with open(file_path, 'r', encoding='utf-8') as f:
                emails = json.load(f)

            if not isinstance(emails, list):
                raise ValueError("Email file must contain a list of emails")

            # Process each email
            for email in emails:
                if not isinstance(email, dict):
                    self.logger.warning("Skipping invalid email object")
                    continue

                self.stats['total_emails'] += 1

                # Check if email has a body
                body = email.get('body', '')
                if not body or body.strip() == '':
                    self.logger.info(f"Skipping email {email.get('id', 'unknown')} - empty body")
                    continue

                # Show progress (blue dot)
                #print('.', end='', flush=True)

                # Classify the email
                classification = self.classify_email(body)

                # Add classification to email object
                email['classification'] = classification
                self.stats['classifications_made'] += 1

                # Append to output file
                self._append_to_output_file(output_file, email)

            # Show completion indicator (green !)
            print('\033[92m!\033[0m', end='', flush=True)

            # Rename original file to .bak
            backup_path = file_path + '.bak'
            os.rename(file_path, backup_path)

            self.stats['files_processed'] += 1
            self.logger.info(f"Completed processing file: {file_path}")

        except json.JSONDecodeError as e:
            error_msg = f"Invalid JSON in email file {file_path}: {e}"
            print(f"Error: {error_msg}")
            self.logger.error(error_msg)
            sys.exit(1)
        except Exception as e:
            error_msg = f"Error processing email file {file_path}: {e}"
            print(f"Error: {error_msg}")
            self.logger.error(error_msg)
            sys.exit(1)

    def _append_to_output_file(self, output_file: str, email: Dict[str, Any]) -> None:
        """Append classified email to output file"""
        try:
            # Check if file exists and has content
            file_exists = os.path.exists(output_file)

            with open(output_file, 'a', encoding='utf-8') as f:
                if not file_exists:
                    # Start JSON array
                    f.write('[\n')
                    json.dump(email, f, indent=2, ensure_ascii=False)
                else:
                    # Add comma and new email (remove last bracket, add comma, new email, close bracket)
                    pass  # We'll handle this differently - read, modify, write

            # Better approach: maintain the file as a proper JSON array
            self._write_email_to_json_array(output_file, email)

        except Exception as e:
            self.logger.error(f"Error writing to output file: {e}")
            raise

    def _write_email_to_json_array(self, output_file: str, email: Dict[str, Any]) -> None:
        """Properly maintain JSON array format in output file"""
        try:
            emails = []

            # Read existing emails if file exists
            if os.path.exists(output_file) and os.path.getsize(output_file) > 0:
                with open(output_file, 'r', encoding='utf-8') as f:
                    try:
                        emails = json.load(f)
                    except json.JSONDecodeError:
                        emails = []  # Start fresh if file is corrupted

            # Add new email
            emails.append(email)

            # Write back to file
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(emails, f, indent=2, ensure_ascii=False)

        except Exception as e:
            self.logger.error(f"Error maintaining JSON array in output file: {e}")
            raise

    def run(self) -> None:
        """Main execution method"""
        try:
            # Log script start
            self.logger.info("EmailClassifier script starting")
            self.logger.info(f"Configuration loaded from settings.json")

            # Validate directories
            self._validate_directories()

            # Load the model
            self.load_model()

            # Get list of email files
            email_files = self.get_email_files()

            if not email_files:
                print("No email files to process.")
                self.logger.info("No email files found - script completed")
                return

            # Create output file name
            timestamp = datetime.now().strftime("%Y%m%d%H%M%S")
            output_filename = f"classified-emails-{timestamp}.json"
            output_path = os.path.join(self.config['ProcessedEmails'], output_filename)

            self.logger.info(f"Processing {len(email_files)} email files")
            self.logger.info(f"Output file: {output_path}")

            # Process each email file
            for email_file in email_files:
                print(f"file: {email_file}")
                self.process_email_file(email_file, output_path)

            # Print completion message
            print('\033[92m\ndone\033[0m')

            # Log completion with statistics
            self.logger.info("EmailClassifier script completed successfully")
            self.logger.info(f"Summary - Files processed: {self.stats['files_processed']}, "
                           f"Emails processed: {self.stats['total_emails']}, "
                           f"Classifications made: {self.stats['classifications_made']}, "
                           f"Errors encountered: {self.stats['errors']}")

        except KeyboardInterrupt:
            print("\nScript interrupted by user")
            self.logger.info("Script interrupted by user")
            sys.exit(1)
        except Exception as e:
            error_msg = f"Unexpected error in main execution: {e}"
            print(f"Error: {error_msg}")
            self.logger.error(error_msg)
            sys.exit(1)

def main():
    """Entry point"""
    print("emailClassifier")
    classifier = EmailClassifier()
    classifier.run()

if __name__ == "__main__":
    main()
