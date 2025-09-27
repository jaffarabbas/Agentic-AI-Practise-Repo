import os
from agents.run import set_default_agent_runner
from dotenv import load_dotenv
from agents import Agent, RunConfig, Runner, set_default_openai_api, set_default_openai_client, set_tracing_disabled
from config import AIProviderConfig

load_dotenv()
set_tracing_disabled(True)
set_default_openai_api("chat_completions")

# Initialize AI provider configuration
ai_config = AIProviderConfig()

# Validate configuration
if not ai_config.validate_config():
    print(f"Error: Missing required environment variables for {ai_config.provider} provider")
    exit(1)

# Get configured client and model
external_client, llm_model = ai_config.get_client_and_model()
print(f"Using {ai_config.provider} provider")

set_default_openai_client(external_client)

config = RunConfig(
    model=llm_model,
    tracing_disabled=True
)

agent: Agent = Agent(
    name="my-agent",
)

agent2: Agent = Agent(
    name="my-agent2",
)

runner = Runner.run_sync(starting_agent=agent, input="what is mcp in ai world?",config=config)
print(runner.final_output)