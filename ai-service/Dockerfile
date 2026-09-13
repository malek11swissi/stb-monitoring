FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
# Les wheels NumPy/SciPy sont volumineux. Si une coupure produit un fichier au
# hash invalide, on ne contourne pas le contrôle : on recommence le téléchargement.
RUN set -e; \
    for attempt in 1 2 3; do \
      pip cache purge >/dev/null 2>&1 || true; \
      if pip install --no-cache-dir --timeout 180 --retries 8 -r requirements.txt; then \
        exit 0; \
      fi; \
      echo "Téléchargement PyPI incomplet (tentative ${attempt}/3), nouvel essai..."; \
      sleep 10; \
    done; \
    exit 1
COPY app ./app
COPY data/resolution_knowledge_base.json ./data/resolution_knowledge_base.json
COPY models ./models
COPY run.py .
RUN addgroup --system --gid 10001 stb && adduser --system --uid 10001 --ingroup stb stb \
    && chown -R stb:stb /app
USER 10001
EXPOSE 5055
CMD ["python","run.py"]
