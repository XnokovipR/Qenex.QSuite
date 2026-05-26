from datetime import datetime, timezone
import psycopg

CONNECTION_STRING = (
    "host=localhost "
    "port=5432 "
    "dbname=qenex_db "
    "user=admin "
    "password=3u4yaVLP4fuAnR8hYX3tp9VQ"
)

def get_variable_id(cursor, name: str) -> int:
    cursor.execute("""
        SELECT variables_id
        FROM public.variables
        WHERE name = %s;
    """, (name,))

    row = cursor.fetchone()

    if row is None:
        raise Exception(f"Variable '{name}' not found in public.variables")

    return row[0]

def insert_variable_value(cursor, variable_id: int, value: float) -> None:
    cursor.execute("""
        INSERT INTO public.variable_values (variables_id, time_stamp, value)
        VALUES (%s, %s, %s);
    """, (variable_id, datetime.now(timezone.utc), value))


db_available = False
postgres_connection = None
try:
    print("Connecting to PostgreSQL...")
    postgres_connection = psycopg.connect(CONNECTION_STRING, connect_timeout=3)
    db_available = True
    print("PostgreSQL connected.")
    
    oil_temp_id = None
    oil_pressure_id = None
    
    with postgres_connection.cursor() as cursor:
        oil_temp_id = get_variable_id(cursor, "OilTemp")
        oil_pressure_id = get_variable_id(cursor, "OilPressure")
    
    print(f"OilTemp id = {oil_temp_id}")
    print(f"OilPressure id = {oil_pressure_id}")

except Exception as ex:
    db_available = False
    postgres_connection = None
    print(f"PostgreSQL connection failed. Runtime continues without DB logging: {ex}")


