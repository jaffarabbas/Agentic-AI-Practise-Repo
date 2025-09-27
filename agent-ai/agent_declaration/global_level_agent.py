import os
from agents.run import set_default_agent_runner
from dotenv import load_dotenv
from agents import Agent, RunConfig, Runner, set_default_openai_api, set_default_openai_client, set_tracing_disabled
from openai import AsyncOpenAI
from config import AIProviderConfig

load_dotenv()
set_tracing_disabled(True)
set_default_openai_api("chat_completions")

external_client = AsyncOpenAI(
    api_key=os.getenv("OPENAI_API_KEY"),
    base_url=os.getenv("OPENAI_API_BASE_URL")
)

set_default_openai_client(external_client)

agent: Agent = Agent(
    model='gpt-4o-mini',
    name="my-agent",
)

runner = Runner.run_sync(starting_agent=agent, input="what is mcp in ai world?")