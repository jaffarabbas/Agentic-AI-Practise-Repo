import os
from dotenv import load_dotenv
from agents import Agent, Runner, set_tracing_disabled
from config import AIProviderConfig

load_dotenv()

# Initialize AI provider configuration
ai_config = AIProviderConfig()

# Validate configuration
if not ai_config.validate_config():
    print(f"Error: Missing required environment variables for {ai_config.provider} provider")
    exit(1)

# Get configured client and model
external_client, llm_model = ai_config.get_client_and_model()
print(f"Using {ai_config.provider} provider")

agent: Agent = Agent(
    name="my-agent",
    model=llm_model
)

runner = Runner.run_sync(starting_agent=agent, input="what is mcp in ai world?")
print(runner.final_output)