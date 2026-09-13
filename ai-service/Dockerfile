FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY app ./app
COPY data/resolution_knowledge_base.json ./data/resolution_knowledge_base.json
COPY models ./models
COPY run.py .
RUN addgroup --system --gid 10001 stb && adduser --system --uid 10001 --ingroup stb stb \
    && chown -R stb:stb /app
USER 10001
EXPOSE 5055
CMD ["python","run.py"]
