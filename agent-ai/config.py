import os
from typing import Dict, Any
from agents import AsyncOpenAI, OpenAIChatCompletionsModel

class AIProviderConfig:
    def __init__(self):
        self.provider = os.getenv("AI_PROVIDER", "gemini").lower()

    def get_client_and_model(self) -> tuple[AsyncOpenAI, OpenAIChatCompletionsModel]:
        """Returns configured client and model based on provider setting"""

        if self.provider == "openai":
            return self._get_openai_config()
        elif self.provider == "gemini":
            return self._get_gemini_config()
        else:
            raise ValueError(f"Unsupported AI provider: {self.provider}")

    def _get_openai_config(self) -> tuple[AsyncOpenAI, OpenAIChatCompletionsModel]:
        """Configure OpenAI client and model"""
        client = AsyncOpenAI(
            api_key=os.getenv("OPENAI_API_KEY"),
            base_url=os.getenv("OPENAI_API_BASE_URL")
        )

        model = OpenAIChatCompletionsModel(
            model="gpt-4o-mini",  # Default OpenAI model
            openai_client=client,
        )

        return client, model

    def _get_gemini_config(self) -> tuple[AsyncOpenAI, OpenAIChatCompletionsModel]:
        """Configure Gemini client and model"""
        client = AsyncOpenAI(
            api_key=os.getenv("GOOGLE_API_KEY"),
            base_url=os.getenv("GOOGLE_API_BASE_URL")
        )

        model = OpenAIChatCompletionsModel(
            model="gemini-2.5-flash",
            openai_client=client,
        )

        return client, model

    def validate_config(self) -> bool:
        """Validate that required environment variables are set"""
        if self.provider == "openai":
            return all([
                os.getenv("OPENAI_API_KEY"),
                os.getenv("OPENAI_API_BASE_URL")
            ])
        elif self.provider == "gemini":
            return all([
                os.getenv("GOOGLE_API_KEY"),
                os.getenv("GOOGLE_API_BASE_URL")
            ])
        return False