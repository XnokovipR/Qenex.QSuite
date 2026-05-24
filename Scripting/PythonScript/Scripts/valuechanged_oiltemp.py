print(f"value changed => {OilTemp.Name}/{oil_temp_id}: {OilTemp.Value}")
with postgres_connection.cursor() as cursor:
	insert_variable_value(cursor, oil_temp_id, OilTemp.Value)
 
postgres_connection.commit()